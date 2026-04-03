// TDD Ciclos 17-19 — GREEN: WaitlistExpiryWorker — rotación por inacción

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Waitlist.Application.Ports;
using Waitlist.Domain.Entities;

namespace Waitlist.Infrastructure.Workers;

public class WaitlistExpiryWorker : BackgroundService
{
    private readonly IWaitlistRepository _repo;
    private readonly IOrderingClient     _ordering;
    private readonly IInventoryClient    _inventory;
    private readonly IEmailService       _email;

    public WaitlistExpiryWorker(
        IWaitlistRepository repo,
        IOrderingClient     ordering,
        IInventoryClient    inventory,
        IEmailService       email)
    {
        _repo      = repo;
        _ordering  = ordering;
        _inventory = inventory;
        _email     = email;
    }

    // ── BackgroundService entrypoint ───────────────────────────────
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await ProcessExpiredEntriesAsync(stoppingToken);
            await Task.Delay(TimeSpan.FromSeconds(60), stoppingToken);
        }
    }

    // ── Core logic — testable independently ───────────────────────
    public async Task ProcessExpiredEntriesAsync(CancellationToken cancellationToken)
    {
        var expired = await _repo.GetExpiredAssignedAsync(cancellationToken);

        foreach (var entry in expired)
        {
            await RotateOrReleaseAsync(entry, cancellationToken);
        }
    }

    private async Task RotateOrReleaseAsync(WaitlistEntry expired, CancellationToken cancellationToken)
    {
        // Ciclo 17/18: expirar el turno actual
        expired.Expire();
        await _repo.UpdateAsync(expired, cancellationToken);

        // Notificar al usuario que perdió su turno
        await _email.SendAsync(
            expired.Email,
            "Tu turno en la lista de espera ha expirado",
            $"No se registró pago en 30 minutos. Tu turno fue liberado. Puedes volver a registrarte.");

        // Buscar siguiente en cola (FIFO)
        var next = await _repo.GetNextPendingAsync(expired.EventId, cancellationToken);

        if (next is not null)
        {
            // Ciclo 17: hay siguiente → reasignar sin liberar al pool
            var newOrderId = await _ordering.CreateWaitlistOrderAsync(
                expired.SeatId!.Value, 0m, next.Email, expired.EventId, cancellationToken);

            next.Assign(expired.SeatId.Value, newOrderId);
            await _repo.UpdateAsync(next, cancellationToken);

            await _email.SendAsync(
                next.Email,
                "Tienes un asiento disponible",
                $"Se te ha asignado un asiento. Tienes 30 minutos para pagar. OrderId: {newOrderId}");
        }
        else
        {
            // Ciclo 18: cola vacía → liberar al inventario general
            await _inventory.ReleaseSeatAsync(expired.SeatId!.Value, cancellationToken);
            await _ordering.CancelOrderAsync(expired.OrderId!.Value, cancellationToken);
        }
    }
}
