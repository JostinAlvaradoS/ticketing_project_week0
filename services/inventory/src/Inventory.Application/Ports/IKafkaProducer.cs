namespace Inventory.Application.Ports;

/// <summary>
/// Puerto para producción de mensajes Kafka (eventos de reserva).
/// </summary>
public interface IKafkaProducer
{
    /// <summary>
    /// Produces a message to the specified Kafka topic.
    /// </summary>
    Task ProduceAsync(string topicName, string message, string? key = null);
}
