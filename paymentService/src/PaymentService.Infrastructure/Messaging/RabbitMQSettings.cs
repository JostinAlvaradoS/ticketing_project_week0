namespace PaymentService.Infrastructure.Messaging;

public class RabbitMQSettings
{
    public const string SectionName = "RabbitMQ";

    public string Host { get; set; } = "localhost";
    public int Port { get; set; } = 5672;
    public string Username { get; set; } = "guest";
    public string Password { get; set; } = "guest";

    public string ApprovedQueueName { get; set; } = "payment-approved-queue";
    public string RejectedQueueName { get; set; } = "payment-rejected-queue";

    /// <summary>
    /// Returns the mapping of queue names to event types for strategy resolution.
    /// To add a new event type: add a queue property, add a constant in PaymentEventTypes,
    /// and add the mapping here.
    /// </summary>
    public Dictionary<string, string> GetQueueEventTypeMappings() => new()
    {
        { ApprovedQueueName, PaymentEventTypes.Approved },
        { RejectedQueueName, PaymentEventTypes.Rejected }
    };
}
