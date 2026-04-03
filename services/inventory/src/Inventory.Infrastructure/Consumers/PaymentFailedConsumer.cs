using System.Text.Json;
using System.Text.Json.Serialization;
using Confluent.Kafka;
using Inventory.Application.Ports;
using Inventory.Domain.Entities;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Inventory.Infrastructure.Consumers;

/// <summary>
/// Consumes `payment-failed` events from Kafka and releases the seat + marks the reservation expired.
/// Prevents permanently blocked seats when a payment attempt fails.
/// </summary>
public class PaymentFailedConsumer : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<PaymentFailedConsumer> _logger;
    private readonly string _bootstrapServers;

    public PaymentFailedConsumer(
        IServiceScopeFactory scopeFactory,
        ILogger<PaymentFailedConsumer> logger,
        string bootstrapServers)
    {
        _scopeFactory = scopeFactory ?? throw new ArgumentNullException(nameof(scopeFactory));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _bootstrapServers = bootstrapServers;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Yield();

        var config = new ConsumerConfig
        {
            BootstrapServers = _bootstrapServers,
            GroupId = "inventory-payment-failed-consumer",
            AutoOffsetReset = AutoOffsetReset.Earliest,
            EnableAutoCommit = true
        };

        using var consumer = new ConsumerBuilder<string?, string>(config)
            .SetErrorHandler((_, e) => _logger.LogError("Kafka consumer error: {Error}", e.Reason))
            .Build();

        consumer.Subscribe("payment-failed");
        _logger.LogInformation("PaymentFailedConsumer subscribed to topic payment-failed");

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var result = consumer.Consume(TimeSpan.FromMilliseconds(500));
                    if (result?.Message?.Value != null)
                        await HandlePaymentFailed(result.Message.Value, stoppingToken);
                }
                catch (ConsumeException ex)
                {
                    _logger.LogError(ex, "Error consuming payment-failed message");
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Unexpected error in PaymentFailedConsumer");
                }
            }
        }
        finally
        {
            consumer.Close();
        }
    }

    private async Task HandlePaymentFailed(string messageJson, CancellationToken cancellationToken)
    {
        PaymentFailedEvent? evt;
        try
        {
            evt = JsonSerializer.Deserialize<PaymentFailedEvent>(messageJson,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to deserialize payment-failed event");
            return;
        }

        if (evt == null || !Guid.TryParse(evt.ReservationId, out var reservationId))
        {
            _logger.LogWarning("payment-failed event missing or invalid reservationId, skipping");
            return;
        }

        using var scope = _scopeFactory.CreateScope();
        var reservationRepo = scope.ServiceProvider.GetRequiredService<IReservationRepository>();
        var seatRepo = scope.ServiceProvider.GetRequiredService<ISeatRepository>();

        var reservation = await reservationRepo.GetByIdAsync(reservationId, cancellationToken);
        if (reservation == null)
        {
            _logger.LogWarning("Reservation {ReservationId} not found for payment-failed event, skipping", reservationId);
            return;
        }

        // Idempotency: skip if already expired
        if (reservation.Status == Reservation.StatusExpired)
        {
            _logger.LogInformation("Reservation {ReservationId} already expired, skipping", reservationId);
            return;
        }

        reservation.Status = Reservation.StatusExpired;
        await reservationRepo.UpdateAsync(reservation, cancellationToken);

        var seat = await seatRepo.GetByIdAsync(reservation.SeatId, cancellationToken);
        if (seat != null && seat.Reserved)
        {
            seat.Release();
            await seatRepo.UpdateAsync(seat, cancellationToken);
            _logger.LogInformation(
                "Seat {SeatId} released after payment failure for reservation {ReservationId}",
                seat.Id, reservationId);
        }
    }

    private sealed class PaymentFailedEvent
    {
        [JsonPropertyName("reservationId")]
        public string? ReservationId { get; init; }

        [JsonPropertyName("orderId")]
        public string? OrderId { get; init; }
    }
}
