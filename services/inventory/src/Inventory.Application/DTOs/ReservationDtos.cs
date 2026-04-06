using System.Text.Json.Serialization;

namespace Inventory.Application.DTOs;

public record CreateReservationRequest(
    Guid SeatId,
    string CustomerId,
    Guid? EventId = null
);

public record CreateReservationResponse(
    Guid ReservationId,
    Guid SeatId,
    string CustomerId,
    DateTime ExpiresAt,
    string Status
);

public record ReservationCreatedEvent(
    [property: JsonPropertyName("eventId")] string EventId,
    [property: JsonPropertyName("reservationId")] string ReservationId,
    [property: JsonPropertyName("customerId")] string CustomerId,
    [property: JsonPropertyName("seatId")] string SeatId,
    [property: JsonPropertyName("seatNumber")] string SeatNumber,
    [property: JsonPropertyName("section")] string Section,
    [property: JsonPropertyName("basePrice")] decimal BasePrice,
    [property: JsonPropertyName("createdAt")] string CreatedAt,
    [property: JsonPropertyName("expiresAt")] string ExpiresAt,
    [property: JsonPropertyName("status")] string Status
);
