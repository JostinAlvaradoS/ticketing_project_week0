using MediatR;

namespace Ordering.Application.UseCases.CancelOrder;

public record CancelOrderCommand(Guid OrderId) : IRequest<bool>;
