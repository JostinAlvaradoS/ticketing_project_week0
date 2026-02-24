using System.Text.Json;
using Confluent.Kafka;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Ordering.Infrastructure.Events;

/// <summary>
/// Background service that consumes reservation events from Kafka.
/// </summary>
public class ReservationEventConsumer : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<ReservationEventConsumer> _logger;
    private readonly KafkaOptions _kafkaOptions;
    private readonly JsonSerializerOptions _jsonOptions;

    public ReservationEventConsumer(
        IServiceProvider serviceProvider,
        ILogger<ReservationEventConsumer> logger,
        IOptions<KafkaOptions> kafkaOptions)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _kafkaOptions = kafkaOptions.Value;
        
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_kafkaOptions.EnableConsumer)
        {
            _logger.LogInformation("Kafka consumer is disabled");
            return;
        }

        var config = new ConsumerConfig
        {
            BootstrapServers = _kafkaOptions.BootstrapServers,
            GroupId = _kafkaOptions.ConsumerGroupId,
            AutoOffsetReset = AutoOffsetReset.Latest,
            EnableAutoCommit = true
        };

        using var consumer = new ConsumerBuilder<string, string>(config)
            .SetErrorHandler((_, e) => _logger.LogError("Kafka consumer error: {Error}", e.Reason))
            .Build();

        try
        {
            consumer.Subscribe(new[] { "reservation-created", "reservation-expired" });
            _logger.LogInformation("Started consuming reservation events from Kafka topics: reservation-created, reservation-expired");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var consumeResult = consumer.Consume(stoppingToken);
                    
                    if (consumeResult?.Message?.Value != null)
                    {
                        await ProcessMessage(consumeResult.Topic, consumeResult.Message.Value, stoppingToken);
                    }
                }
                catch (ConsumeException ex)
                {
                    _logger.LogError(ex, "Error consuming Kafka message");
                }
                catch (JsonException ex)
                {
                    _logger.LogError(ex, "Error deserializing Kafka message");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Unexpected error processing Kafka message");
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fatal error in Kafka consumer");
        }
        finally
        {
            consumer.Close();
            _logger.LogInformation("Kafka consumer closed");
        }
    }

    private async Task ProcessMessage(string topic, string messageValue, CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var reservationStore = scope.ServiceProvider.GetRequiredService<ReservationStore>();

        switch (topic)
        {
            case "reservation-created":
                var createdEvent = JsonSerializer.Deserialize<ReservationCreatedEvent>(messageValue, _jsonOptions);
                
                if (createdEvent != null)
                {
                    reservationStore.AddReservation(createdEvent);
                    _logger.LogInformation("Processed reservation-created event for reservation {ReservationId}", 
                        createdEvent.ReservationId);
                }
                break;

            case "reservation-expired":
                var expiredEvent = JsonSerializer.Deserialize<ReservationExpiredEvent>(messageValue, _jsonOptions);
                
                if (expiredEvent != null)
                {
                    reservationStore.RemoveReservation(expiredEvent);
                    _logger.LogInformation("Processed reservation-expired event for reservation {ReservationId}", 
                        expiredEvent.ReservationId);
                }
                break;

            default:
                _logger.LogWarning("Unknown topic: {Topic}", topic);
                break;
        }

        await Task.CompletedTask;
    }
}

/// <summary>
/// Configuration options for Kafka.
/// </summary>
public class KafkaOptions
{
    public const string Section = "Kafka";
    
    public string BootstrapServers { get; set; } = "localhost:9092";
    public string ConsumerGroupId { get; set; } = "ordering-service";
    public bool EnableConsumer { get; set; } = true;
}