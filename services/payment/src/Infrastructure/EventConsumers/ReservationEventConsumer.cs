using Confluent.Kafka;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Payment.Infrastructure.Events;
using Payment.Infrastructure.Services;
using System.Text.Json;

namespace Payment.Infrastructure.EventConsumers;

/// <summary>
/// Background service that consumes reservation events from Kafka to maintain local state.
/// Eliminates the need for HTTP calls to inventory service for reservation validation.
/// </summary>
public class ReservationEventConsumer : BackgroundService
{
    private readonly KafkaOptions _kafkaOptions;
    private readonly ReservationStateStore _reservationStore;
    private readonly ILogger<ReservationEventConsumer> _logger;
    private IConsumer<string?, string>? _consumer;

    public ReservationEventConsumer(
        IOptions<KafkaOptions> kafkaOptions,
        ReservationStateStore reservationStore,
        ILogger<ReservationEventConsumer> logger)
    {
        _kafkaOptions = kafkaOptions.Value;
        _reservationStore = reservationStore;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_kafkaOptions.EnableConsumer)
        {
            _logger.LogInformation("Kafka consumer is disabled, skipping reservation event consumption");
            return;
        }

        _logger.LogInformation("Starting reservation event consumer for payment service");

        try
        {
            var config = new ConsumerConfig
            {
                BootstrapServers = _kafkaOptions.BootstrapServers,
                GroupId = _kafkaOptions.ConsumerGroupId,
                AutoOffsetReset = AutoOffsetReset.Earliest,
                EnableAutoCommit = true,
                EnableAutoOffsetStore = false
            };

            _consumer = new ConsumerBuilder<string?, string>(config).Build();
            _consumer.Subscribe(new[] { "reservation-created", "reservation-expired" });

            _logger.LogInformation("Subscribed to reservation topics: reservation-created, reservation-expired");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var consumeResult = _consumer.Consume(TimeSpan.FromMilliseconds(1000));
                    if (consumeResult?.Message?.Value != null)
                    {
                        await ProcessReservationEvent(consumeResult, stoppingToken);
                        _consumer.StoreOffset(consumeResult);
                    }

                    // Periodically cleanup expired reservations
                    _reservationStore.CleanupExpiredReservations();
                }
                catch (OperationCanceledException)
                {
                    _logger.LogInformation("Reservation event consumer cancellation requested");
                    break;
                }
                catch (ConsumeException ex)
                {
                    _logger.LogError(ex, "Error consuming reservation event: {Error}", ex.Error.Reason);
                    await Task.Delay(5000, stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Unexpected error in reservation event consumer");
                    await Task.Delay(5000, stoppingToken);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fatal error in reservation event consumer");
        }
        finally
        {
            _consumer?.Close();
            _consumer?.Dispose();
        }
    }

    private async Task ProcessReservationEvent(ConsumeResult<string?, string> result, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogDebug("Processing reservation event from topic {Topic}: {Message}", result.Topic, result.Message.Value);

            switch (result.Topic)
            {
                case "reservation-created":
                    await HandleReservationCreated(result.Message.Value, cancellationToken);
                    break;
                case "reservation-expired":
                    await HandleReservationExpired(result.Message.Value, cancellationToken);
                    break;
                default:
                    _logger.LogWarning("Unknown reservation topic: {Topic}", result.Topic);
                    break;
            }
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to deserialize reservation event from topic {Topic}", result.Topic);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing reservation event from topic {Topic}", result.Topic);
        }
    }

    private Task HandleReservationCreated(string eventJson, CancellationToken cancellationToken)
    {
        var reservationEvent = JsonSerializer.Deserialize<ReservationCreatedEvent>(eventJson, new JsonSerializerOptions 
        { 
            PropertyNameCaseInsensitive = true 
        });

        if (reservationEvent == null)
        {
            _logger.LogWarning("Failed to deserialize reservation-created event");
            return Task.CompletedTask;
        }

        if (!Guid.TryParse(reservationEvent.ReservationId, out var reservationId) ||
            !Guid.TryParse(reservationEvent.CustomerId, out var customerId) ||
            !Guid.TryParse(reservationEvent.SeatId, out var seatId))
        {
            _logger.LogWarning("Invalid GUIDs in reservation-created event");
            return Task.CompletedTask;
        }

        _reservationStore.AddReservation(reservationId, customerId, seatId, reservationEvent.ExpiresAt);
        
        _logger.LogInformation("Processed reservation-created event for reservation {ReservationId}", reservationId);
        return Task.CompletedTask;
    }

    private Task HandleReservationExpired(string eventJson, CancellationToken cancellationToken)
    {
        var expiredEvent = JsonSerializer.Deserialize<ReservationExpiredEvent>(eventJson, new JsonSerializerOptions 
        { 
            PropertyNameCaseInsensitive = true 
        });

        if (expiredEvent == null)
        {
            _logger.LogWarning("Failed to deserialize reservation-expired event");
            return Task.CompletedTask;
        }

        if (!Guid.TryParse(expiredEvent.ReservationId, out var reservationId))
        {
            _logger.LogWarning("Invalid ReservationId GUID in reservation-expired event");
            return Task.CompletedTask;
        }

        _reservationStore.ExpireReservation(reservationId);
        
        _logger.LogInformation("Processed reservation-expired event for reservation {ReservationId}", reservationId);
        return Task.CompletedTask;
    }

    public override void Dispose()
    {
        _consumer?.Close();
        _consumer?.Dispose();
        base.Dispose();
    }
}