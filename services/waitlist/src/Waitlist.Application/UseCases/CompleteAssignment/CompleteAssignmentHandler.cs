// TDD Ciclo 16 — GREEN: pago exitoso → Completed

using MediatR;
using Waitlist.Application.Ports;

namespace Waitlist.Application.UseCases.CompleteAssignment;

public class CompleteAssignmentHandler : IRequestHandler<CompleteAssignmentCommand>
{
    private readonly IWaitlistRepository _repo;

    public CompleteAssignmentHandler(IWaitlistRepository repo)
    {
        _repo = repo;
    }

    public async Task Handle(CompleteAssignmentCommand command, CancellationToken cancellationToken)
    {
        var entry = await _repo.GetByOrderIdAsync(command.OrderId, cancellationToken);
        if (entry is null) return; // Ciclo 16b: idempotencia — no es una orden de waitlist

        entry.Complete();
        await _repo.UpdateAsync(entry, cancellationToken);
    }
}
