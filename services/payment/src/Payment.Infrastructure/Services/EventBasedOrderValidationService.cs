using Microsoft.Extensions.Logging;
using Payment.Application.Ports;

namespace Payment.Infrastructure.Services;

/// <summary>
/// Event-driven implementation of order validation using reservation events.
/// Orders are considered valid if they have active reservations.
/// Replaces HTTP calls to ordering service with local state maintained by ReservationEventConsumer.
/// </summary>
public class EventBasedOrderValidationService : IOrderValidationService
{
    private readonly ReservationStateStore _reservationStore;
    private readonly ILogger<EventBasedOrderValidationService> _logger;

    public EventBasedOrderValidationService(
        ReservationStateStore reservationStore, 
        ILogger<EventBasedOrderValidationService> logger)
    {
        _reservationStore = reservationStore ?? throw new ArgumentNullException(nameof(reservationStore));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public Task<OrderValidationResult> ValidateOrderAsync(Guid orderId, Guid customerId, decimal expectedAmount, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Validating order {OrderId} for customer {CustomerId} with expected amount {ExpectedAmount} using reservation event state", 
            orderId, customerId, expectedAmount);

        // In this simplified EDA approach, we validate orders through their associated reservations
        // If the reservation store has valid reservations for this customer, the order is considered valid
        var hasValidReservations = _reservationStore.HasActiveReservationsForCustomer(customerId);
        
        OrderValidationResult result;
        
        if (hasValidReservations)
        {
            result = new OrderValidationResult(true, null, orderId, "pending", expectedAmount);
            _logger.LogDebug("Order {OrderId} validation successful - customer has active reservations", orderId);
        }
        else
        {
            result = new OrderValidationResult(false, "No active reservations found for customer");
            _logger.LogDebug("Order {OrderId} validation failed: No active reservations for customer {CustomerId}", orderId, customerId);
        }

        return Task.FromResult(result);
    }
}