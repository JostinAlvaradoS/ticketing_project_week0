using Microsoft.Extensions.Logging;
using Payment.Application.Ports;
using System.Collections.Concurrent;

namespace Payment.Infrastructure.Services;

/// <summary>
/// In-memory store for active reservations based on Kafka events.
/// Maintains local state of reservations for validation without HTTP calls.
/// </summary>
public class ReservationStateStore
{
    private readonly ConcurrentDictionary<Guid, ReservationState> _reservations = new();
    private readonly ILogger<ReservationStateStore> _logger;

    public ReservationStateStore(ILogger<ReservationStateStore> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public void AddReservation(Guid reservationId, Guid customerId, Guid seatId, DateTime expiresAt)
    {
        var reservation = new ReservationState(reservationId, customerId, seatId, expiresAt, "active", DateTime.UtcNow);
        _reservations.AddOrUpdate(reservationId, reservation, (_, _) => reservation);
        
        _logger.LogDebug("Added reservation {ReservationId} for customer {CustomerId}, expires at {ExpiresAt}", 
            reservationId, customerId, expiresAt);
    }

    public void ExpireReservation(Guid reservationId)
    {
        if (_reservations.TryGetValue(reservationId, out var existing))
        {
            var expired = existing with { Status = "expired" };
            _reservations.AddOrUpdate(reservationId, expired, (_, _) => expired);
            
            _logger.LogDebug("Expired reservation {ReservationId}", reservationId);
        }
    }

    public ReservationValidationResult ValidateReservation(Guid reservationId, Guid customerId)
    {
        if (!_reservations.TryGetValue(reservationId, out var reservation))
        {
            return new ReservationValidationResult(false, "Reservation not found");
        }

        // Check ownership
        if (reservation.CustomerId != customerId)
        {
            return new ReservationValidationResult(false, "Reservation does not belong to the specified customer");
        }

        // Check status
        if (reservation.Status != "active")
        {
            return new ReservationValidationResult(false, $"Reservation is in '{reservation.Status}' state, expected 'active'");
        }

        // Check expiration
        if (reservation.ExpiresAt <= DateTime.UtcNow)
        {
            return new ReservationValidationResult(false, "Reservation has expired");
        }

        return new ReservationValidationResult(true, null, reservation.ReservationId, reservation.SeatId, reservation.ExpiresAt, reservation.Status);
    }

    public bool HasActiveReservationsForCustomer(Guid customerId)
    {
        return _reservations.Values.Any(reservation => 
            reservation.CustomerId == customerId && 
            reservation.Status == "active" && 
            reservation.ExpiresAt > DateTime.UtcNow);
    }

    public void CleanupExpiredReservations()
    {
        var cutoff = DateTime.UtcNow;
        var expiredKeys = _reservations.Where(kvp => kvp.Value.ExpiresAt <= cutoff && kvp.Value.Status == "active")
                                     .Select(kvp => kvp.Key)
                                     .ToList();

        foreach (var key in expiredKeys)
        {
            if (_reservations.TryGetValue(key, out var reservation))
            {
                var expired = reservation with { Status = "expired" };
                _reservations.TryUpdate(key, expired, reservation);
            }
        }

        if (expiredKeys.Any())
        {
            _logger.LogDebug("Auto-expired {Count} reservations due to TTL", expiredKeys.Count);
        }
    }

    public int GetActiveReservationCount() => _reservations.Count(kvp => kvp.Value.Status == "active");
}

public record ReservationState(
    Guid ReservationId,
    Guid CustomerId,
    Guid SeatId,
    DateTime ExpiresAt,
    string Status,
    DateTime CreatedAt
);