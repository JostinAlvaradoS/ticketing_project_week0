using MediatR;
using Ordering.Application.Exceptions;
using Ordering.Application.Ports;
using Ordering.Domain.Entities;

namespace Ordering.Application.UseCases.CreateWaitlistOrder;

public class CreateWaitlistOrderHandler : IRequestHandler<CreateWaitlistOrderCommand, CreateWaitlistOrderResult>
{
    private readonly IOrderRepository _repository;

    public CreateWaitlistOrderHandler(IOrderRepository repository)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
    }

    public async Task<CreateWaitlistOrderResult> Handle(CreateWaitlistOrderCommand request, CancellationToken cancellationToken)
    {
        var existing = await _repository.GetActiveOrderBySeatIdAsync(request.SeatId, cancellationToken)
            .ConfigureAwait(false);

        if (existing is not null)
            throw new DuplicateSeatOrderException(request.SeatId);

        var order = Order.Create(null, request.GuestToken);
        order.AddItem(request.SeatId, request.Price);
        order.Checkout(); // waitlist orders skip the draft phase and go directly to pending

        var created = await _repository.CreateAsync(order, cancellationToken).ConfigureAwait(false);

        return new CreateWaitlistOrderResult(created.Id);
    }
}
