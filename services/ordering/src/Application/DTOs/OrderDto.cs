namespace Ordering.Application.DTOs;

public record OrderDto(
    Guid Id,
    string? UserId,
    string? GuestToken,
    decimal TotalAmount,
    string State,
    DateTime CreatedAt,
    DateTime? PaidAt,
    IEnumerable<OrderItemDto> Items
);

public record OrderItemDto(
    Guid Id,
    Guid SeatId,
    decimal Price
);