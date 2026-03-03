namespace Payment.Application.Ports;

/// <summary>
/// Port for validating reservations before payment processing.
/// </summary>
public interface IReservationValidationService
{
    /// <summary>
    /// Validates that the reservation exists, is active, and not expired.
    /// </summary>
    /// <param name="reservationId">Reservation ID to validate</param>
    /// <param name="customerId">Customer ID that should own the reservation</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Validation result with reservation details if valid</returns>
    Task<ReservationValidationResult> ValidateReservationAsync(Guid reservationId, Guid customerId, CancellationToken cancellationToken = default);
}

public record ReservationValidationResult(
    bool IsValid,
    string? ErrorMessage,
    Guid? ReservationId = null,
    Guid? SeatId = null,
    DateTime? ExpiresAt = null,
    string? Status = null
);