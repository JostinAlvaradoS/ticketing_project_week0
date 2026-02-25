/// <summary>
/// Configuration options for Kafka.
/// </summary>
namespace Payment.Infrastructure.Events;

public class KafkaOptions
{
    public const string Section = "Kafka";
    
    public string BootstrapServers { get; set; } = "localhost:9092";
    public string ConsumerGroupId { get; set; } = "payment-service";
    public bool EnableConsumer { get; set; } = true;
}