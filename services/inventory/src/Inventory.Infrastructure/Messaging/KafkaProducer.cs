using Confluent.Kafka;
using Inventory.Application.Ports;

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

        try
        {
            // Use a 5-second timeout for Kafka produce
            using var cts = new System.Threading.CancellationTokenSource(TimeSpan.FromSeconds(5));
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
                // Don't throw — let the request succeed even if Kafka publish fails
            }
        }
        catch (OperationCanceledException)
        {
            Console.WriteLine($"[KafkaProducer] Timeout producing to {topicName} — continuing without wait");
            // Don't throw — let the Kafka publish fail silently and return response
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[KafkaProducer] Error producing to {topicName}: {ex.Message}");
            // Don't throw — let request succeed even if Kafka fails
        }
    }

    public async ValueTask DisposeAsync()
    {
        _producer?.Flush(TimeSpan.FromSeconds(5));
        _producer?.Dispose();
        await Task.CompletedTask;
    }
}
