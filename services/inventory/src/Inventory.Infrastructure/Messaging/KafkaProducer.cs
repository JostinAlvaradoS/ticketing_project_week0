using Confluent.Kafka;
using Inventory.Domain.Ports;

namespace Inventory.Infrastructure.Messaging;

/// <summary>
/// Kafka producer adapter for publishing domain events (reservation-created, etc.)
/// </summary>
public class KafkaProducer : IKafkaProducer, IAsyncDisposable
{
    private readonly IProducer<string?, string> _producer;

    public KafkaProducer(IProducer<string?, string> producer)
    {
        _producer = producer ?? throw new ArgumentNullException(nameof(producer));
    }

    public async Task ProduceAsync(string topicName, string message, string? key = null)
    {
        if (string.IsNullOrEmpty(topicName)) throw new ArgumentNullException(nameof(topicName));
        if (string.IsNullOrEmpty(message)) throw new ArgumentNullException(nameof(message));

        var deliveryReport = await _producer.ProduceAsync(
            topicName,
            new Message<string?, string>
            {
                Key = key,
                Value = message
            }).ConfigureAwait(false);

        if (deliveryReport.Status != PersistenceStatus.Persisted)
        {
            throw new InvalidOperationException($"Failed to produce message to {topicName}: {deliveryReport.Status}");
        }
    }

    public async ValueTask DisposeAsync()
    {
        _producer?.Flush(TimeSpan.FromSeconds(5));
        _producer?.Dispose();
        await Task.CompletedTask;
    }
}
