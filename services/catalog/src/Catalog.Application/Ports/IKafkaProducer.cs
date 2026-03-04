namespace Catalog.Application.Ports;

/// <summary>
/// Port for producing Kafka messages (seats-generated events, etc.).
/// </summary>
public interface IKafkaProducer
{
    /// <summary>
    /// Produces a message to a Kafka topic.
    /// </summary>
    /// <param name="topicName">Topic name (e.g., "seats-generated")</param>
    /// <param name="message">Serialized message JSON</param>
    /// <param name="key">Optional message key for partitioning</param>
    Task ProduceAsync(string topicName, string message, string? key = null);
}
