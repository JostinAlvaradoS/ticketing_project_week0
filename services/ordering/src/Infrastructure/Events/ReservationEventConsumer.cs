using System.Text.Json;
using System.Text.Json.Serialization;
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

        // Wait a bit for Kafka to be ready in Docker environments
        await Task.Delay(2000, stoppingToken);

        int retryCount = 0;
        const int maxRetries = 5;
        const int retryDelayMs = 3000;

        while (!stoppingToken.IsCancellationRequested && retryCount < maxRetries)
        {
            try
            {
                await ConsumeMessagesAsync(stoppingToken);
                retryCount = 0; // Reset on successful connection
            }
            catch (Exception ex)
            {
                retryCount++;
                _logger.LogWarning(ex, "Kafka consumer error (attempt {RetryCount}/{MaxRetries}). Retrying in {DelayMs}ms...", 
                    retryCount, maxRetries, retryDelayMs);
                
                if (retryCount < maxRetries)
                {
                    await Task.Delay(retryDelayMs, stoppingToken);
                }
            }
        }

        if (retryCount >= maxRetries)
        {
            _logger.LogError("Kafka consumer failed after {MaxRetries} attempts. Shutting down.", maxRetries);
        }
    }

    private async Task ConsumeMessagesAsync(CancellationToken stoppingToken)
    {
        var config = new ConsumerConfig
        {
            BootstrapServers = _kafkaOptions.BootstrapServers,
            GroupId = _kafkaOptions.ConsumerGroupId,
            // Use Earliest in smoke-tests / MVP so consumer reads messages
            // that may have been published before the service subscribed.
            AutoOffsetReset = AutoOffsetReset.Earliest,
            EnableAutoCommit = true,
            ConnectionsMaxIdleMs = 30000
        };

        using var consumer = new ConsumerBuilder<string, string>(config)
            .SetErrorHandler((_, e) => _logger.LogError("Kafka consumer error: {Error}", e.Reason))
            .Build();

        try
        {
            consumer.Subscribe(new[] { "reservation-created", "reservation-expired", "payment-succeeded" });
            _logger.LogInformation("Started consuming reservation and payment events from Kafka topics");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    // HUMAN CHECK: Consumo de eventos con reintento manual.
                    // Se identificó deuda técnica: se recomienda migrar a Polly para manejar
                    // reintentos con Exponential Backoff y Circuit Breaker.
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
                catch (OperationCanceledException)
                {
                    break;
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
            throw;
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

            case "payment-succeeded":
                var paymentEvent = JsonSerializer.Deserialize<PaymentSucceededEvent>(messageValue, _jsonOptions);
                if (paymentEvent != null && Guid.TryParse(paymentEvent.OrderId, out var orderId))
                {
                    var orderRepo = scope.ServiceProvider.GetRequiredService<Ordering.Application.Ports.IOrderRepository>();
                    var order = await orderRepo.GetByIdAsync(orderId, cancellationToken);
                    if (order != null)
                    {
                        order.State = "paid";
                        order.PaidAt = DateTime.UtcNow;
                        await orderRepo.UpdateAsync(order, cancellationToken);
                        _logger.LogInformation("Order {OrderId} updated to State: PAID", orderId);
                    }
                }
                break;

            default:
                _logger.LogWarning("Unknown topic: {Topic}", topic);
                break;
        }

        await Task.CompletedTask;
    }

    private class PaymentSucceededEvent
    {
        [JsonPropertyName("orderId")]
        public string OrderId { get; set; } = string.Empty;
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