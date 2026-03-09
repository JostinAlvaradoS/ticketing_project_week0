namespace Payment.Application.Ports;

/// <summary>
/// Port for publishing payment events to Kafka.
/// </summary>
public interface IKafkaProducer
{
    /// <summary>
    /// Publishes a message to the specified Kafka topic.
    /// </summary>
    /// <param name="topicName">Kafka topic name</param>
    /// <param name="message">JSON message to publish</param>
    /// <param name="key">Optional message key</param>
    /// <returns>Task that completes when message is published</returns>
    Task ProduceAsync(string topicName, string message, string? key = null);
}