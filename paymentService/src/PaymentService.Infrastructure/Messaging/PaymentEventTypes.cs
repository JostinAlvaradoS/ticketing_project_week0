namespace PaymentService.Infrastructure.Messaging;

/// <summary>
/// Constants for payment event types used by the Strategy pattern.
/// Maps to the queue names in RabbitMQSettings for routing.
/// To add a new event type, define the constant here and create
/// a corresponding IPaymentEventStrategy implementation.
/// </summary>
public static class PaymentEventTypes
{
    public const string Approved = "payment.approved";
    public const string Rejected = "payment.rejected";
}
