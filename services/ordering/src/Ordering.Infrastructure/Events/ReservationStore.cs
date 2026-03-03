using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using Ordering.Application.Ports;

namespace Ordering.Infrastructure.Events;

/// <summary>
/// In-memory store for active reservations based on Kafka events.
/// Implements reservation validation without external HTTP calls.
/// </summary>
public class ReservationStore : IReservationValidationService
{
    private readonly ConcurrentDictionary<Guid, ActiveReservation> _reservations = new();
    private readonly ILogger<ReservationStore> _logger;

    public ReservationStore(ILogger<ReservationStore> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Adds or updates a reservation from a reservation-created event.
    /// </summary>
    public void AddReservation(ReservationCreatedEvent evt)
    {
        if (!Guid.TryParse(evt.ReservationId, out var reservationId) ||
            !Guid.TryParse(evt.SeatId, out var seatId))
        {
            _logger.LogWarning("Invalid GUID format in reservation event: {ReservationId}, {SeatId}", 
                evt.ReservationId, evt.SeatId);
            return;
        }

        var reservation = new ActiveReservation(
            reservationId,
            seatId,
            evt.CustomerId,
            evt.ExpiresAt,
            evt.Status,
            evt.CreatedAt
        );

        _reservations.AddOrUpdate(reservationId, reservation, (key, old) => reservation);
        
        _logger.LogInformation("Added reservation {ReservationId} for seat {SeatId} expiring at {ExpiresAt}",
            reservationId, seatId, evt.ExpiresAt);
    }

    /// <summary>
    /// Removes a reservation from a reservation-expired event.
    /// </summary>
    public void RemoveReservation(ReservationExpiredEvent evt)
    {
        if (!Guid.TryParse(evt.ReservationId, out var reservationId))
        {
            _logger.LogWarning("Invalid reservation ID format: {ReservationId}", evt.ReservationId);
            return;
        }

        if (_reservations.TryRemove(reservationId, out var removed))
        {
            _logger.LogInformation("Removed expired reservation {ReservationId} for seat {SeatId}",
                reservationId, removed.SeatId);
        }
        else
        {
            _logger.LogWarning("Attempted to remove non-existent reservation {ReservationId}", reservationId);
        }
    }

    public Task<ReservationValidationResult> ValidateReservationAsync(Guid? reservationId, Guid seatId)
    {
        // If no reservation ID provided, check if seat is available (no active reservation)
        if (reservationId == null)
        {
            var hasActiveReservation = _reservations.Values
                .Any(r => r.SeatId == seatId && r.ExpiresAt > DateTime.UtcNow && r.Status == "active");
            
            if (hasActiveReservation)
            {
                return Task.FromResult(new ReservationValidationResult(
                    false, "Seat has an active reservation", null));
            }
            
            return Task.FromResult(new ReservationValidationResult(true, null, null));
        }

        // Validate specific reservation
        if (!_reservations.TryGetValue(reservationId.Value, out var reservation))
        {
            _logger.LogWarning("Reservation {ReservationId} not found in store", reservationId);
            return Task.FromResult(new ReservationValidationResult(
                false, "Reservation not found", null));
        }

        // Check if reservation matches the seat
        if (reservation.SeatId != seatId)
        {
            _logger.LogWarning("Reservation {ReservationId} is for seat {ReservationSeatId} but requested for seat {SeatId}",
                reservationId, reservation.SeatId, seatId);
            return Task.FromResult(new ReservationValidationResult(
                false, "Reservation is for a different seat", null));
        }

        // Check if reservation has expired
        if (reservation.ExpiresAt <= DateTime.UtcNow)
        {
            _logger.LogWarning("Reservation {ReservationId} has expired at {ExpiresAt}",
                reservationId, reservation.ExpiresAt);
            
            // Clean up expired reservation
            _reservations.TryRemove(reservationId.Value, out _);
            
            return Task.FromResult(new ReservationValidationResult(
                false, "Reservation has expired", null));
        }

        // Check reservation status
        if (reservation.Status != "active")
        {
            _logger.LogWarning("Reservation {ReservationId} is not active, status: {Status}",
                reservationId, reservation.Status);
            return Task.FromResult(new ReservationValidationResult(
                false, $"Reservation is {reservation.Status}", null));
        }

        var details = new ReservationDetails(
            reservation.ReservationId,
            reservation.SeatId,
            reservation.CustomerId,
            reservation.ExpiresAt,
            reservation.Status
        );

        _logger.LogInformation("Reservation {ReservationId} validated successfully for seat {SeatId}",
            reservationId, seatId);
        
        return Task.FromResult(new ReservationValidationResult(true, null, details));
    }

    public Task<bool> HasActiveReservationAsync(Guid seatId)
    {
        var hasActive = _reservations.Values
            .Any(r => r.SeatId == seatId && r.ExpiresAt > DateTime.UtcNow && r.Status == "active");
        
        return Task.FromResult(hasActive);
    }

    /// <summary>
    /// Gets count of active reservations (for monitoring/debugging).
    /// </summary>
    public int GetActiveReservationCount()
    {
        return _reservations.Values.Count(r => r.ExpiresAt > DateTime.UtcNow && r.Status == "active");
    }

    /// <summary>
    /// Cleans up expired reservations (can be called periodically).
    /// </summary>
    public void CleanupExpiredReservations()
    {
        var now = DateTime.UtcNow;
        var expired = _reservations
            .Where(kvp => kvp.Value.ExpiresAt <= now)
            .Select(kvp => kvp.Key)
            .ToList();

        foreach (var expiredId in expired)
        {
            if (_reservations.TryRemove(expiredId, out var removed))
            {
                _logger.LogInformation("Cleaned up expired reservation {ReservationId} for seat {SeatId}",
                    expiredId, removed.SeatId);
            }
        }
    }

    private record ActiveReservation(
        Guid ReservationId,
        Guid SeatId,
        string? CustomerId,
        DateTime ExpiresAt,
        string Status,
        DateTime CreatedAt
    );
}