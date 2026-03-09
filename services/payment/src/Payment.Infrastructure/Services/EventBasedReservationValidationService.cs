using Microsoft.Extensions.Logging;
using Payment.Application.Ports;

namespace Payment.Infrastructure.Services;

/// <summary>
/// Event-driven implementation of reservation validation using local state from Kafka events.
/// Replaces HTTP calls to inventory service with local state maintained by ReservationEventConsumer.
/// </summary>
public class EventBasedReservationValidationService : IReservationValidationService
{
    private readonly ReservationStateStore _reservationStore;
    private readonly ILogger<EventBasedReservationValidationService> _logger;

    public EventBasedReservationValidationService(
        ReservationStateStore reservationStore, 
        ILogger<EventBasedReservationValidationService> logger)
    {
        _reservationStore = reservationStore ?? throw new ArgumentNullException(nameof(reservationStore));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public Task<ReservationValidationResult> ValidateReservationAsync(Guid reservationId, Guid customerId, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Validating reservation {ReservationId} for customer {CustomerId} using local event state", 
            reservationId, customerId);

        var result = _reservationStore.ValidateReservation(reservationId, customerId);
        
        if (result.IsValid)
        {
            _logger.LogDebug("Reservation {ReservationId} validation successful using event-driven approach", reservationId);
        }
        else
        {
            _logger.LogDebug("Reservation {ReservationId} validation failed: {Error}", reservationId, result.ErrorMessage);
        }

        return Task.FromResult(result);
    }
}