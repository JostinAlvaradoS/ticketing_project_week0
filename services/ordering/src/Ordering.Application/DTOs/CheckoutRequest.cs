namespace Ordering.Application.DTOs;

public record CheckoutRequest(
    Guid OrderId,
    string? UserId = null,
    string? GuestToken = null
);