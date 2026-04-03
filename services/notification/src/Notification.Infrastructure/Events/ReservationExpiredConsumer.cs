using System.Text.Json;
using System.Text.Json.Serialization;
using Confluent.Kafka;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Notification.Application.Ports;

namespace Notification.Infrastructure.Events;

/// <summary>
/// Consumes `reservation-expired` events and dispatches a notification to the affected customer.
/// Requires the enriched v2 event payload that includes customerId.
/// </summary>
public class ReservationExpiredConsumer : BackgroundService
{
    private readonly KafkaOptions _kafkaOptions;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<ReservationExpiredConsumer> _logger;

    public ReservationExpiredConsumer(
        IOptions<KafkaOptions> kafkaOptions,
        IServiceProvider serviceProvider,
        ILogger<ReservationExpiredConsumer> logger)
    {
        _kafkaOptions = kafkaOptions.Value;
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Yield();

        var config = new ConsumerConfig
        {
            BootstrapServers = _kafkaOptions.BootstrapServers,
            GroupId = "notification-reservation-expired-consumer",
            AutoOffsetReset = AutoOffsetReset.Earliest,
            EnableAutoCommit = true
        };

        IConsumer<string, string>? consumer = null;
        try
        {
            consumer = new ConsumerBuilder<string, string>(config)
                .SetErrorHandler((_, e) => _logger.LogError("Kafka error: {Error}", e.Reason))
                .Build();
            consumer.Subscribe("reservation-expired");
            _logger.LogInformation("ReservationExpiredConsumer subscribed to topic reservation-expired");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create Kafka consumer for reservation-expired. Notifications will not be sent.");
            return;
        }

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var result = consumer.Consume(TimeSpan.FromMilliseconds(500));
                    if (result?.Message?.Value != null)
                        await HandleReservationExpired(result.Message.Value, stoppingToken);
                }
                catch (ConsumeException ex)
                {
                    _logger.LogError(ex, "Error consuming reservation-expired message");
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Unexpected error in ReservationExpiredConsumer");
                }
            }
        }
        finally
        {
            consumer.Close();
            consumer.Dispose();
        }
    }

    private async Task HandleReservationExpired(string messageJson, CancellationToken _)
    {
        ReservationExpiredEvent? evt;
        try
        {
            evt = JsonSerializer.Deserialize<ReservationExpiredEvent>(messageJson,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to deserialize reservation-expired event");
            return;
        }

        if (evt == null)
        {
            _logger.LogWarning("Deserialized reservation-expired event is null, skipping");
            return;
        }

        if (string.IsNullOrWhiteSpace(evt.CustomerId))
        {
            _logger.LogWarning(
                "reservation-expired event {ReservationId} has no customerId — notification skipped",
                evt.ReservationId);
            return;
        }

        _logger.LogInformation(
            "Dispatching reservation-expired notification to customer {CustomerId} for reservation {ReservationId}",
            evt.CustomerId, evt.ReservationId);

        using var scope = _serviceProvider.CreateScope();
        var emailService = scope.ServiceProvider.GetService<IEmailService>();

        if (emailService != null)
        {
            try
            {
                // customerId is used as the recipient address; a customer resolver
                // would map this to an actual email in a production system.
                await emailService.SendAsync(
                    recipientEmail: evt.CustomerId,
                    subject: "Your seat reservation has expired",
                    body: $"Your reservation {evt.ReservationId} for seat {evt.SeatId} has expired " +
                          $"because payment was not completed in time. The seat is now available again.");

                _logger.LogInformation(
                    "Reservation-expired notification dispatched for customer {CustomerId}",
                    evt.CustomerId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Failed to send reservation-expired notification to customer {CustomerId}",
                    evt.CustomerId);
            }
        }
        else
        {
            _logger.LogWarning("IEmailService not registered — reservation-expired notification not sent for customer {CustomerId}", evt.CustomerId);
        }
    }

    private sealed class ReservationExpiredEvent
    {
        [JsonPropertyName("reservationId")]
        public string? ReservationId { get; init; }

        [JsonPropertyName("seatId")]
        public string? SeatId { get; init; }

        [JsonPropertyName("customerId")]
        public string? CustomerId { get; init; }
    }
}
