namespace Inventory.Domain.Entities;

/// <summary>
/// Represents a reserved seat with TTL (15 minutes). Once expired, a background worker will handle cleanup.
/// </summary>
public class Reservation
{
    /// <summary>
    /// Unique reservation identifier (primary key).
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Reference to the seat being reserved.
    /// </summary>
    public Guid SeatId { get; set; }

    /// <summary>
    /// Customer/user who made the reservation.
    /// </summary>
    public string CustomerId { get; set; } = string.Empty;

    /// <summary>
    /// When the reservation was created.
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// When the reservation expires (typically now + 15 minutes).
    /// </summary>
    public DateTime ExpiresAt { get; set; }

    /// <summary>
    /// Status: active, expired, or confirmed.
    /// </summary>
    public string Status { get; set; } = "active";

    /// <summary>
    /// Checks if the reservation has exceeded its TTL (15 minutes).
    /// </summary>
    public bool IsExpired(DateTime currentTime) => 
        Status == "expired" || currentTime > CreatedAt.AddMinutes(15);
}
