using Confluent.Kafka;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using Fulfillment.Application.Ports;

namespace Fulfillment.Infrastructure.Events;

public class KafkaEventPublisher : IEventPublisher
{
    private readonly IProducer<string, string> _producer;
    private readonly ILogger<KafkaEventPublisher> _logger;
    private readonly ProducerConfig _config;

    public KafkaEventPublisher(IOptions<KafkaOptions> kafkaOptions, ILogger<KafkaEventPublisher> logger)
    {
        _logger = logger;
        
        _config = new ProducerConfig
        {
            BootstrapServers = kafkaOptions.Value.BootstrapServers
        };

        // No instanciamos el productor aquí para evitar problemas en tiempo de diseño (EF Core)
    }

    private IProducer<string, string> GetProducer()
    {
        // El productor se crea perezosamente al primer intento de uso
        return _producer ?? new ProducerBuilder<string, string>(_config).Build();
    }

    public async Task<bool> PublishAsync<T>(string topic, string key, T @event) where T : class
    {
        try
        {
            var json = JsonSerializer.Serialize(@event, new JsonSerializerOptions 
            { 
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase 
            });

            var message = new Message<string, string>
            {
                Key = key,
                Value = json
            };

            var producer = GetProducer();
            var result = await producer.ProduceAsync(topic, message);
            
            _logger.LogInformation($"Evento publicado en {topic}: {key}");
            return result.Status == PersistenceStatus.Persisted;
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error publicando evento en {topic}: {ex.Message}");
            return false;
        }
    }
}
