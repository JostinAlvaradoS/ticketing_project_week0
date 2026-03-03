namespace Notification.Infrastructure.Events;

public class KafkaOptions
{
    public const string Section = "Kafka";
    public string BootstrapServers { get; set; } = "localhost:9092";
    public string ConsumerGroupId { get; set; } = "notification-service";
    public Dictionary<string, string> Topics { get; set; } = new();
}
