namespace Fulfillment.Application.Ports;

/// <summary>
/// Puerto para publicación de eventos a Kafka.
/// </summary>
public interface IEventPublisher
{
    Task<bool> PublishAsync<T>(string topic, string key, T @event) where T : class;
}
