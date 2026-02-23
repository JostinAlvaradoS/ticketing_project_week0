namespace Inventory.Application.DTOs;

public record CreateReservationRequest(
    Guid SeatId,
    string CustomerId
);

public record CreateReservationResponse(
    Guid ReservationId,
    Guid SeatId,
    string CustomerId,
    DateTime ExpiresAt,
    string Status
);

public record ReservationCreatedEvent(
    string EventId,
    string ReservationId,
    string CustomerId,
    string SeatId,
    string SeatNumber,
    string Section,
    decimal BasePrice,
    string CreatedAt,
    string ExpiresAt,
    string Status
);
