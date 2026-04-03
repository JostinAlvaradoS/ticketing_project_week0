// TDD Ciclos 12-14 — GREEN: mínimo para pasar tests de asignación automática

using MediatR;
using Waitlist.Application.Ports;

namespace Waitlist.Application.UseCases.AssignNext;

public class AssignNextHandler : IRequestHandler<AssignNextCommand>
{
    private readonly IWaitlistRepository _repo;
    private readonly IOrderingClient     _ordering;
    private readonly IEmailService       _email;

    public AssignNextHandler(IWaitlistRepository repo, IOrderingClient ordering, IEmailService email)
    {
        _repo     = repo;
        _ordering = ordering;
        _email    = email;
    }

    public async Task Handle(AssignNextCommand command, CancellationToken cancellationToken)
    {
        // Ciclo 14: idempotencia — si el asiento ya fue asignado, skip
        var alreadyAssigned = await _repo.HasAssignedEntryForSeatAsync(command.SeatId, cancellationToken);
        if (alreadyAssigned) return;

        // Ciclo 13: cola vacía → no acción
        var next = await _repo.GetNextPendingAsync(command.ConcertEventId, cancellationToken);
        if (next is null) return;

        // Ciclo 12: crear orden + asignar + notificar
        var orderId = await _ordering.CreateWaitlistOrderAsync(
            command.SeatId, 0m, next.Email, command.ConcertEventId, cancellationToken);

        next.Assign(command.SeatId, orderId);
        await _repo.UpdateAsync(next, cancellationToken);

        await _email.SendAsync(
            next.Email,
            "Tienes un asiento disponible",
            $"Se te ha asignado un asiento. Tienes 30 minutos para completar el pago. OrderId: {orderId}");
    }
}
