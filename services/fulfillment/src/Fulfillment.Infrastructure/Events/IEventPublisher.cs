namespace Fulfillment.Infrastructure.Events;

public interface IEventPublisher
{
    /// <summary>
    /// Publica un evento a Kafka
    /// </summary>
    Task<bool> PublishAsync<T>(string topic, string key, T @event) where T : class;
}
