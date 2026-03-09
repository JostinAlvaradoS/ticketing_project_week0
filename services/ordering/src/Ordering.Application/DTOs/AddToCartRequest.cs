namespace Ordering.Application.DTOs;

public record AddToCartRequest(
    Guid? ReservationId,
    Guid SeatId,
    decimal Price,
    string? UserId = null,
    string? GuestToken = null
);