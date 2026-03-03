using System.Text.Json.Serialization;

namespace Payment.Infrastructure.Events;

/// <summary>
/// Event published when a seat reservation is successfully created.
/// Matches the schema defined in contracts/kafka/reservation-created.json
/// </summary>
public record ReservationCreatedEvent(
    [property: JsonPropertyName("eventId")] string EventId,
    [property: JsonPropertyName("reservationId")] string ReservationId,
    [property: JsonPropertyName("customerId")] string CustomerId,
    [property: JsonPropertyName("seatId")] string SeatId,
    [property: JsonPropertyName("seatNumber")] string SeatNumber,
    [property: JsonPropertyName("section")] string Section,
    [property: JsonPropertyName("basePrice")] decimal BasePrice,
    [property: JsonPropertyName("createdAt")] DateTime CreatedAt,
    [property: JsonPropertyName("expiresAt")] DateTime ExpiresAt,
    [property: JsonPropertyName("status")] string Status
);

/// <summary>
/// Event published when a reservation expires.
/// Matches the schema defined in contracts/kafka/reservation-expired.json
/// </summary>
public record ReservationExpiredEvent(
    [property: JsonPropertyName("reservationId")] string ReservationId,
    [property: JsonPropertyName("eventId")] string EventId,
    [property: JsonPropertyName("seatId")] string SeatId,
    [property: JsonPropertyName("customerId")] string CustomerId,
    [property: JsonPropertyName("expiredAt")] DateTime ExpiredAt,
    [property: JsonPropertyName("reason")] string Reason,
    [property: JsonPropertyName("status")] string Status
);