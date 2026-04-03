using MediatR;
using Waitlist.Application.Ports;

namespace Waitlist.Application.UseCases.AssignNext;

public class AssignNextHandler : IRequestHandler<AssignNextCommand>
{
    private readonly IWaitlistRepository _repository;
    private readonly IOrderingClient _orderingClient;
    private readonly IEmailService _emailService;

    public AssignNextHandler(
        IWaitlistRepository repository,
        IOrderingClient orderingClient,
        IEmailService emailService)
    {
        _repository = repository;
        _orderingClient = orderingClient;
        _emailService = emailService;
    }

    public async Task Handle(AssignNextCommand request, CancellationToken cancellationToken)
    {
        // Idempotency: skip if seat already has an assigned entry
        var alreadyAssigned = await _repository.HasAssignedEntryForSeatAsync(request.SeatId, cancellationToken);
        if (alreadyAssigned) return;

        var next = await _repository.GetNextPendingAsync(request.EventId, cancellationToken);
        if (next is null) return;

        var orderId = await _orderingClient.CreateWaitlistOrderAsync(
            next.Email, request.SeatId, request.EventId, cancellationToken);

        next.Assign(request.SeatId, orderId);
        await _repository.UpdateAsync(next, cancellationToken);

        await _emailService.SendWaitlistAssignmentAsync(
            next.Email, request.SeatId, next.ExpiresAt!.Value, cancellationToken);
    }
}
