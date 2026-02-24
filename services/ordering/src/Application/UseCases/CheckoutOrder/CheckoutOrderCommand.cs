using MediatR;

namespace Ordering.Application.UseCases.CheckoutOrder;

public record CheckoutOrderCommand(
    Guid OrderId,
    string? UserId = null,
    string? GuestToken = null
) : IRequest<CheckoutOrderResponse>;