using MediatR;
using Ordering.Application.DTOs;

namespace Ordering.Application.UseCases.AddToCart;

public record AddToCartCommand(
    Guid? ReservationId,
    Guid SeatId,
    decimal Price,
    string? UserId = null,
    string? GuestToken = null
) : IRequest<AddToCartResponse>;