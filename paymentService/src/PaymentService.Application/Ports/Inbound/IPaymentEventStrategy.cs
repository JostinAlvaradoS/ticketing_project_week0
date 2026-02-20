using PaymentService.Application.Dtos;

namespace PaymentService.Application.Ports.Inbound;

/// <summary>
/// Strategy pattern interface for handling different payment event types.
/// Each implementation handles a specific event type (approved, rejected, etc.).
/// Adding a new event type only requires implementing this interface and registering it in DI.
/// </summary>
public interface IPaymentEventStrategy
{
    /// <summary>
    /// Unique identifier for the event type this strategy handles.
    /// Examples: "payment.approved", "payment.rejected"
    /// </summary>
    string EventType { get; }

    /// <summary>
    /// Processes the raw JSON payload of the payment event.
    /// </summary>
    Task<ValidationResult> HandleAsync(string payload, CancellationToken cancellationToken = default);
}
