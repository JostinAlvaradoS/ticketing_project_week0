// TDD Ciclos 17-19 — GREEN: WaitlistExpiryWorker — rotación por inacción

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Waitlist.Application.Ports;
using Waitlist.Domain.Entities;

namespace Waitlist.Infrastructure.Workers;

public class WaitlistExpiryWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;

    public WaitlistExpiryWorker(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }

    // ── BackgroundService entrypoint ───────────────────────────────
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Yield so the host finishes startup (Kestrel binds) before we hit the DB
        await Task.Yield();

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessExpiredEntriesAsync(stoppingToken);
            }
            catch (OperationCanceledException) { break; }
            catch (Exception ex)
            {
                // Log and continue — never crash the host
                Console.Error.WriteLine($"[WaitlistExpiryWorker] Error: {ex.Message}");
            }

            await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
        }
    }

    // ── Core logic — testable independently ───────────────────────
    public async Task ProcessExpiredEntriesAsync(CancellationToken cancellationToken)
    {
        // All scoped services resolved per cycle to avoid captive dependency
        using var scope    = _scopeFactory.CreateScope();
        var repo           = scope.ServiceProvider.GetRequiredService<IWaitlistRepository>();
        var ordering       = scope.ServiceProvider.GetRequiredService<IOrderingClient>();
        var inventory      = scope.ServiceProvider.GetRequiredService<IInventoryClient>();
        var email          = scope.ServiceProvider.GetRequiredService<IEmailService>();

        var expired = await repo.GetExpiredAssignedAsync(cancellationToken);

        foreach (var entry in expired)
        {
            await RotateOrReleaseAsync(repo, ordering, inventory, email, entry, cancellationToken);
        }
    }

    private static async Task RotateOrReleaseAsync(
        IWaitlistRepository repo,
        IOrderingClient     ordering,
        IInventoryClient    inventory,
        IEmailService       email,
        WaitlistEntry       expired,
        CancellationToken   cancellationToken)
    {
        // Ciclo 17/18: expirar el turno actual
        expired.Expire();
        await repo.UpdateAsync(expired, cancellationToken);

        await email.SendAsync(
            expired.Email,
            "Tu turno en la lista de espera ha expirado",
            $"No se registró pago en 30 minutos. Tu turno fue liberado. Puedes volver a registrarte.");

        // Buscar siguiente en cola (FIFO)
        var next = await repo.GetNextPendingAsync(expired.EventId, cancellationToken);

        if (next is not null)
        {
            // Ciclo 17: hay siguiente → cancelar orden anterior y reasignar al siguiente
            await ordering.CancelOrderAsync(expired.OrderId!.Value, cancellationToken);

            var newOrderId = await ordering.CreateWaitlistOrderAsync(
                expired.SeatId!.Value, 0m, next.Email, expired.EventId, cancellationToken);

            next.Assign(expired.SeatId.Value, newOrderId);
            await repo.UpdateAsync(next, cancellationToken);

            await email.SendAsync(
                next.Email,
                "Tienes un asiento disponible",
                $"Se te ha asignado un asiento. Tienes 30 minutos para pagar. OrderId: {newOrderId}");
        }
        else
        {
            // Ciclo 18: cola vacía → liberar al inventario general
            await inventory.ReleaseSeatAsync(expired.SeatId!.Value, cancellationToken);
            await ordering.CancelOrderAsync(expired.OrderId!.Value, cancellationToken);
        }
    }
}
