using System.Text.Json.Serialization;

namespace Ordering.Infrastructure.Events;

/// <summary>
/// Event published when a seat reservation is created.
/// Maps to the reservation-created.json schema.
/// </summary>
public record ReservationCreatedEvent
{
    [JsonPropertyName("eventId")]
    public string EventId { get; init; } = string.Empty;
    
    [JsonPropertyName("reservationId")]
    public string ReservationId { get; init; } = string.Empty;
    
    [JsonPropertyName("customerId")]
    public string? CustomerId { get; init; }
    
    [JsonPropertyName("seatId")]
    public string SeatId { get; init; } = string.Empty;
    
    [JsonPropertyName("seatNumber")]
    public string SeatNumber { get; init; } = string.Empty;
    
    [JsonPropertyName("section")]
    public string Section { get; init; } = string.Empty;
    
    [JsonPropertyName("basePrice")]
    public decimal BasePrice { get; init; }
    
    [JsonPropertyName("createdAt")]
    public DateTime CreatedAt { get; init; }
    
    [JsonPropertyName("expiresAt")]
    public DateTime ExpiresAt { get; init; }
    
    [JsonPropertyName("status")]
    public string Status { get; init; } = "active";
}

/// <summary>
/// Event published when a reservation expires (v2 — includes customerId).
/// </summary>
public record ReservationExpiredEvent
{
    [JsonPropertyName("reservationId")]
    public string ReservationId { get; init; } = string.Empty;

    [JsonPropertyName("seatId")]
    public string SeatId { get; init; } = string.Empty;

    [JsonPropertyName("customerId")]
    public string? CustomerId { get; init; }

    [JsonPropertyName("expiredAt")]
    public DateTime ExpiredAt { get; init; }
}