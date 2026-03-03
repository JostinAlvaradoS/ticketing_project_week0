namespace Fulfillment.Infrastructure.Events;

public class KafkaOptions
{
    public const string Section = "Kafka";
    
    public string BootstrapServers { get; set; } = string.Empty;
    public string ConsumerGroupId { get; set; } = string.Empty;
    public Dictionary<string, string> Topics { get; set; } = new();
}
