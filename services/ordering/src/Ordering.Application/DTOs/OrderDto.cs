namespace Ordering.Application.DTOs;

public record OrderDto(
    Guid Id,
    string? UserId,
    string? GuestToken,
    decimal TotalAmount,
    string State,
    DateTime CreatedAt,
    DateTime? PaidAt,
    IEnumerable<OrderItemDto> Items,
    string? EventName = null,
    string? SeatNumber = null,
    Guid EventId = default
);

public record OrderItemDto(
    Guid Id,
    Guid SeatId,
    decimal Price
);