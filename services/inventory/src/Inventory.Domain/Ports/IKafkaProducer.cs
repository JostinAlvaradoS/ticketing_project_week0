namespace Inventory.Domain.Ports;

/// <summary>
/// Port for producing Kafka messages (reservation-created events).
/// </summary>
public interface IKafkaProducer
{
    /// <summary>
    /// Produces a reservation-created event to Kafka.
    /// </summary>
    /// <param name="topicName">Topic name (e.g., "reservation-created")</param>
    /// <param name="message">Serialized message JSON</param>
    /// <param name="key">Optional message key</param>
    /// <returns>A task that completes when the message is produced</returns>
    Task ProduceAsync(string topicName, string message, string? key = null);
}
