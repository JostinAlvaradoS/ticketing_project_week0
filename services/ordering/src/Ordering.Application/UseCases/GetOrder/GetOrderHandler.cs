using MediatR;
using Ordering.Application.DTOs;
using Ordering.Application.Ports;

namespace Ordering.Application.UseCases.GetOrder;

public record GetOrderQuery(Guid OrderId) : IRequest<OrderDto?>;

public class GetOrderHandler : IRequestHandler<GetOrderQuery, OrderDto?>
{
    private readonly IOrderRepository _orderRepository;

    public GetOrderHandler(IOrderRepository orderRepository)
    {
        _orderRepository = orderRepository;
    }

    public async Task<OrderDto?> Handle(GetOrderQuery request, CancellationToken cancellationToken)
    {
        var order = await _orderRepository.GetByIdAsync(request.OrderId, cancellationToken);
        
        if (order == null)
            return null;

        // ENRICHMENT: In a real scenario, we would call Catalog and Identity service here.
        // For the current MVP scope and testing consistency, we ensure the DTO reflects 
        // the data needed by Fulfillment.
        var firstItem = order.Items.FirstOrDefault();

        return new OrderDto(
            order.Id,
            order.UserId,
            order.GuestToken,
            order.TotalAmount,
            order.State,
            order.CreatedAt,
            order.PaidAt,
            order.Items.Select(i => new OrderItemDto(i.Id, i.SeatId, i.Price)),
            EventName: null, // TODO: enriquecer desde Catalog service
            SeatNumber: firstItem != null ? $"Seat-{firstItem.SeatId.ToString()[..4]}" : null,
            EventId: Guid.Empty
        );
    }
}
