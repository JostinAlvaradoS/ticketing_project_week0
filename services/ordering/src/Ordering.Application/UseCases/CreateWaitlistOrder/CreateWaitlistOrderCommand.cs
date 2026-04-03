using MediatR;

namespace Ordering.Application.UseCases.CreateWaitlistOrder;

public record CreateWaitlistOrderCommand(Guid SeatId, string GuestToken, decimal Price)
    : IRequest<CreateWaitlistOrderResult>;

public record CreateWaitlistOrderResult(Guid OrderId);
