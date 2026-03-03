using Confluent.Kafka;
using Microsoft.Extensions.Logging;
using Payment.Application.Ports;

namespace Payment.Infrastructure.Messaging;

/// <summary>
/// Kafka producer adapter for publishing payment events.
/// </summary>
public class KafkaProducer : IKafkaProducer, IAsyncDisposable
{
    private readonly IProducer<string?, string> _producer;
    private readonly ILogger<KafkaProducer> _logger;

    public KafkaProducer(IProducer<string?, string> producer, ILogger<KafkaProducer> logger)
    {
        _producer = producer ?? throw new ArgumentNullException(nameof(producer));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task ProduceAsync(string topicName, string message, string? key = null)
    {
        if (string.IsNullOrEmpty(topicName)) throw new ArgumentNullException(nameof(topicName));
        if (string.IsNullOrEmpty(message)) throw new ArgumentNullException(nameof(message));

        try
        {
            // Use a 5-second timeout for Kafka produce
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            var deliveryReport = await _producer.ProduceAsync(
                topicName,
                new Message<string?, string>
                {
                    Key = key,
                    Value = message
                },
                cts.Token);

            _logger.LogDebug("Message delivered to {Topic} [{Partition}] at offset {Offset}",
                deliveryReport.Topic, deliveryReport.Partition, deliveryReport.Offset);
        }
        catch (OperationCanceledException)
        {
            _logger.LogError("Kafka produce timed out for topic {Topic}", topicName);
            throw new InvalidOperationException($"Failed to deliver message to topic '{topicName}': timeout");
        }
        catch (ProduceException<string?, string> ex)
        {
            _logger.LogError(ex, "Failed to deliver message to topic {Topic}: {Error}", topicName, ex.Error.Reason);
            throw new InvalidOperationException($"Failed to deliver message to topic '{topicName}': {ex.Error.Reason}");
        }
    }

    public ValueTask DisposeAsync()
    {
        _producer?.Flush(TimeSpan.FromSeconds(10));
        _producer?.Dispose();
        return ValueTask.CompletedTask;
    }
}