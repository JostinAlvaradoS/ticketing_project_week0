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
        var order = await _orderRepository.GetByIdAsync(request.OrderId, cancellationToken);

        if (order == null)
        {
            return new CheckoutOrderResponse(false, "Order not found", null);
        }

        if (!order.BelongsTo(request.UserId, request.GuestToken))
        {
            return new CheckoutOrderResponse(false, "Unauthorized", null);
        }

        // La lógica de validación (estado Draft, items no vacíos) está en order.Checkout()
        // Las excepciones de dominio se convierten a respuestas de fallo — las de infraestructura propagan
        try
        {
            order.Checkout();
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("empty"))
        {
            return new CheckoutOrderResponse(false, "Order is empty", null);
        }
        catch (InvalidOperationException)
        {
            return new CheckoutOrderResponse(false, "Order is not in draft state", null);
        }

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
}
