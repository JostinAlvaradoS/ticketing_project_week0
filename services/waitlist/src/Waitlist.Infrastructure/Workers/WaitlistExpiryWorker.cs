using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Waitlist.Application.Ports;
using Waitlist.Application.UseCases.AssignNext;
using Waitlist.Domain.Entities;

namespace Waitlist.Infrastructure.Workers;

/// <summary>
/// Background worker that polls for expired Assigned entries and handles rotation or seat release.
/// Uses IServiceScopeFactory to avoid scoped-in-singleton DI lifetime issues.
/// </summary>
public class WaitlistExpiryWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly TimeSpan _pollInterval;

    public WaitlistExpiryWorker(IServiceScopeFactory scopeFactory)
        : this(scopeFactory, TimeSpan.FromSeconds(60)) { }

    public WaitlistExpiryWorker(IServiceScopeFactory scopeFactory, TimeSpan pollInterval)
    {
        _scopeFactory = scopeFactory;
        _pollInterval = pollInterval;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Yield();

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessExpiredEntriesAsync(stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[WaitlistExpiryWorker] Error: {ex.Message}");
            }

            await Task.Delay(_pollInterval, stoppingToken);
        }
    }

    public async Task ProcessExpiredEntriesAsync(CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<IWaitlistRepository>();
        var ordering = scope.ServiceProvider.GetRequiredService<IOrderingClient>();
        var inventory = scope.ServiceProvider.GetRequiredService<IInventoryClient>();
        var email = scope.ServiceProvider.GetRequiredService<IEmailService>();

        var expired = await repo.GetExpiredAssignedAsync(cancellationToken);
        foreach (var entry in expired)
            await RotateOrReleaseAsync(repo, ordering, inventory, email, entry, cancellationToken);
    }

    private static async Task RotateOrReleaseAsync(
        IWaitlistRepository repo,
        IOrderingClient ordering,
        IInventoryClient inventory,
        IEmailService email,
        WaitlistEntry expired,
        CancellationToken cancellationToken)
    {
        // Cancel existing order if any
        if (expired.OrderId.HasValue)
        {
            try { await ordering.CancelOrderAsync(expired.OrderId.Value, cancellationToken); }
            catch (Exception ex) { Console.Error.WriteLine($"[WaitlistExpiryWorker] CancelOrder failed: {ex.Message}"); }
        }

        expired.Expire();
        await repo.UpdateAsync(expired, cancellationToken);

        // Try next pending in queue
        var next = await repo.GetNextPendingAsync(expired.EventId, cancellationToken);
        if (next is not null && expired.SeatId.HasValue)
        {
            try
            {
                var newOrderId = await ordering.CreateWaitlistOrderAsync(
                    next.Email, expired.SeatId.Value, expired.EventId, cancellationToken);
                next.Assign(expired.SeatId.Value, newOrderId);
                await repo.UpdateAsync(next, cancellationToken);
                await email.SendWaitlistAssignmentAsync(
                    next.Email, expired.SeatId.Value, next.ExpiresAt!.Value, cancellationToken);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[WaitlistExpiryWorker] Rotate failed: {ex.Message}");
            }
        }
        else if (expired.SeatId.HasValue)
        {
            // No one waiting — release the seat back to inventory
            try { await inventory.ReleaseSeatAsync(expired.SeatId.Value, cancellationToken); }
            catch (Exception ex) { Console.Error.WriteLine($"[WaitlistExpiryWorker] ReleaseSeat failed: {ex.Message}"); }
        }
    }
}
