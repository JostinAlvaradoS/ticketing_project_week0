namespace Ordering.Application.Ports;

/// <summary>
/// Service for managing and validating seat reservations based on Kafka events.
/// </summary>
public interface IReservationValidationService
{
    /// <summary>
    /// Validates that a reservation exists and is still active.
    /// </summary>
    /// <param name="reservationId">The reservation ID to validate</param>
    /// <param name="seatId">The seat ID that should be reserved</param>
    /// <returns>Validation result</returns>
    Task<ReservationValidationResult> ValidateReservationAsync(Guid? reservationId, Guid seatId);

    /// <summary>
    /// Checks if a seat has an active reservation.
    /// </summary>
    /// <param name="seatId">The seat ID to check</param>
    /// <returns>True if seat has an active reservation</returns>
    Task<bool> HasActiveReservationAsync(Guid seatId);
}

/// <summary>
/// Result of reservation validation.
/// </summary>
public record ReservationValidationResult(
    bool IsValid,
    string? ErrorMessage,
    ReservationDetails? ReservationDetails
);

/// <summary>
/// Details about a validated reservation.
/// </summary>
public record ReservationDetails(
    Guid ReservationId,
    Guid SeatId,
    string? CustomerId,
    DateTime ExpiresAt,
    string Status
);