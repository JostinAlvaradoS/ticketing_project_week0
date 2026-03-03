using MediatR;
using Ordering.Application.DTOs;
using Ordering.Application.Ports;

namespace Ordering.Application.UseCases.CheckoutOrder;

public sealed class CheckoutOrderHandler : IRequestHandler<CheckoutOrderCommand, CheckoutOrderResponse>
{
    private readonly IOrderRepository _orderRepository;

    public CheckoutOrderHandler(IOrderRepository orderRepository)
    {
        _orderRepository = orderRepository;
    }

    public async Task<CheckoutOrderResponse> Handle(CheckoutOrderCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var order = await _orderRepository.GetByIdAsync(request.OrderId, cancellationToken);
            
            if (order == null)
            {
                return new CheckoutOrderResponse(false, "Order not found", null);
            }

            // Validate ownership
            if (!string.IsNullOrEmpty(request.UserId) && order.UserId != request.UserId)
            {
                return new CheckoutOrderResponse(false, "Unauthorized", null);
            }

            if (!string.IsNullOrEmpty(request.GuestToken) && order.GuestToken != request.GuestToken)
            {
                return new CheckoutOrderResponse(false, "Unauthorized", null);
            }

            // Validate order state
            if (order.State != "draft")
            {
                return new CheckoutOrderResponse(false, "Order is not in draft state", null);
            }

            if (!order.Items.Any())
            {
                return new CheckoutOrderResponse(false, "Order is empty", null);
            }

            // Update order state to pending (ready for payment)
            order.State = "pending";
            
            var updatedOrder = await _orderRepository.UpdateAsync(order, cancellationToken);

            var orderDto = new OrderDto(
                updatedOrder.Id,
                updatedOrder.UserId,
                updatedOrder.GuestToken,
                updatedOrder.TotalAmount,
                updatedOrder.State,
                updatedOrder.CreatedAt,
                updatedOrder.PaidAt,
                updatedOrder.Items.Select(i => new OrderItemDto(i.Id, i.SeatId, i.Price))
            );

            return new CheckoutOrderResponse(true, null, orderDto);
        }
        catch (Exception ex)
        {
            return new CheckoutOrderResponse(false, $"Failed to checkout order: {ex.Message}", null);
        }
    }
}