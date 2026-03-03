using Confluent.Kafka;
using Catalog.Application.Ports;

namespace Catalog.Infrastructure.Messaging;

/// <summary>
/// Kafka producer adapter for publishing domain events (seats-generated, etc.)
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

        try
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            var deliveryReport = await _producer.ProduceAsync(
                topicName,
                new Message<string?, string>
                {
                    Key = key,
                    Value = message
                },
                cts.Token).ConfigureAwait(false);

            if (deliveryReport.Status != PersistenceStatus.Persisted)
            {
                Console.WriteLine($"[KafkaProducer] Failed to produce to {topicName}: {deliveryReport.Status}");
            }
        }
        catch (OperationCanceledException)
        {
            Console.WriteLine($"[KafkaProducer] Timeout producing to {topicName} — continuing without wait");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[KafkaProducer] Error producing to {topicName}: {ex.Message}");
        }
    }

    public async ValueTask DisposeAsync()
    {
        _producer?.Flush(TimeSpan.FromSeconds(5));
        _producer?.Dispose();
        await Task.CompletedTask;
    }
}
