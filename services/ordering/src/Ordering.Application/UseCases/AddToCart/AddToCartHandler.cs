using MediatR;
using Ordering.Application.DTOs;
using Ordering.Application.Ports;
using Ordering.Domain.Entities;

namespace Ordering.Application.UseCases.AddToCart;

public sealed class AddToCartHandler : IRequestHandler<AddToCartCommand, AddToCartResponse>
{
    private readonly IOrderRepository _orderRepository;
    private readonly IReservationValidationService _reservationValidationService;

    public AddToCartHandler(
        IOrderRepository orderRepository,
        IReservationValidationService reservationValidationService)
    {
        _orderRepository = orderRepository;
        _reservationValidationService = reservationValidationService;
    }

    public async Task<AddToCartResponse> Handle(AddToCartCommand request, CancellationToken cancellationToken)
    {
        var validationResult = await _reservationValidationService.ValidateReservationAsync(
            request.ReservationId,
            request.SeatId);

        if (!validationResult.IsValid)
        {
            return new AddToCartResponse(false, validationResult.ErrorMessage, null);
        }

        var existingOrder = await _orderRepository.GetDraftOrderAsync(
            request.UserId, request.GuestToken, cancellationToken);

        Order order;

        if (existingOrder != null)
        {
            // Seat-already-in-cart es una regla de negocio del dominio — se convierte a respuesta de fallo
            try
            {
                existingOrder.AddItem(request.SeatId, request.Price);
            }
            catch (InvalidOperationException)
            {
                return new AddToCartResponse(false, "Seat is already in the cart", null);
            }

            order = await _orderRepository.UpdateAsync(existingOrder, cancellationToken);
        }
        else
        {
            order = Order.Create(request.UserId, request.GuestToken);
            order.AddItem(request.SeatId, request.Price);
            order = await _orderRepository.CreateAsync(order, cancellationToken);
        }

        var orderDto = MapToDto(order);
        return new AddToCartResponse(true, null, orderDto);
    }

    private static OrderDto MapToDto(Order order)
    {
        return new OrderDto(
            order.Id,
            order.UserId,
            order.GuestToken,
            order.TotalAmount,
            order.State,
            order.CreatedAt,
            order.PaidAt,
            order.Items.Select(i => new OrderItemDto(i.Id, i.SeatId, i.Price))
        );
    }
}
