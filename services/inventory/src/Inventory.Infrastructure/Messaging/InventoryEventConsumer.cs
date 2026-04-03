using System.Text.Json;
using System.Text.Json.Serialization;
using Confluent.Kafka;
using Inventory.Domain.Entities;
using Inventory.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Inventory.Infrastructure.Messaging;

/// <summary>
/// Consumes payment events to update seat reservation status.
/// </summary>
public class InventoryEventConsumer : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<InventoryEventConsumer> _logger;
    private readonly string _bootstrapServers;
    private readonly string _topic = "payment-succeeded";
    private readonly string _groupId = "inventory-service-group";

    public InventoryEventConsumer(
        IServiceProvider serviceProvider,
        ILogger<InventoryEventConsumer> logger,
        Microsoft.Extensions.Configuration.IConfiguration configuration)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        // CORRECT: configuration.GetConnectionString("Default") or similar is usually a helper, but for 
        // raw strings from Config we use indexers.
        _bootstrapServers = configuration.GetSection("Kafka")["BootstrapServers"] ?? "localhost:9092";
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("InventoryEventConsumer starting...");

        // Wait a bit for Kafka to be ready
        await Task.Delay(5000, stoppingToken);

        var config = new ConsumerConfig
        {
            BootstrapServers = _bootstrapServers,
            GroupId = _groupId,
            AutoOffsetReset = AutoOffsetReset.Earliest,
            EnableAutoCommit = true,
            SessionTimeoutMs = 30000
        };

        using var consumer = new ConsumerBuilder<string, string>(config).Build();
        consumer.Subscribe(_topic);

        _logger.LogInformation("Subscribed to the topic {Topic}", _topic);

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var result = consumer.Consume(TimeSpan.FromSeconds(10));
                    if (result == null) continue;

                    _logger.LogInformation("Received event: {Topic} {Partition} {Offset}", result.Topic, result.Partition, result.Offset);

                    using var scope = _serviceProvider.CreateScope();
                    var dbContext = scope.ServiceProvider.GetRequiredService<InventoryDbContext>();

                    var paymentSucceeded = JsonSerializer.Deserialize<PaymentSucceededEvent>(result.Message.Value, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

                    if (paymentSucceeded != null && !string.IsNullOrEmpty(paymentSucceeded.ReservationId))
                    {
                        if (Guid.TryParse(paymentSucceeded.ReservationId, out var reservationId))
                        {
                            var reservation = await dbContext.Reservations
                                .FirstOrDefaultAsync(r => r.Id == reservationId, stoppingToken);

                            if (reservation != null)
                            {
                                reservation.Status = Reservation.StatusConfirmed;
                                _logger.LogInformation("Reservation {ReservationId} confirmed for Seat {SeatId}", reservation.Id, reservation.SeatId);

                                // UPDATE SEAT STATUS
                                var seat = await dbContext.Seats.FindAsync(new object[] { reservation.SeatId }, stoppingToken);
                                if (seat != null)
                                {
                                    if (!seat.Reserved) seat.Reserve(); // Ensure it stays reserved (sold)
                                    _logger.LogInformation("Seat {SeatId} status confirmed as RESERVED (Sold)", seat.Id);
                                }

                                await dbContext.SaveChangesAsync(stoppingToken);
                            }
                            else
                            {
                                _logger.LogWarning("Reservation {ReservationId} not found in inventory db", reservationId);
                            }
                        }
                        else
                        {
                            _logger.LogWarning("Invalid ReservationId format: {ReservationId}", paymentSucceeded.ReservationId);
                        }
                    }
                }
                catch (ConsumeException ex)
                {
                    _logger.LogError(ex, "Error consuming Kafka message");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing Inventory event");
                }
            }
        }
        finally
        {
            consumer.Close();
        }
    }

    private class PaymentSucceededEvent
    {
        [JsonPropertyName("reservationId")]
        public string? ReservationId { get; set; }

        [JsonPropertyName("orderId")]
        public string? OrderId { get; set; }

        [JsonPropertyName("paymentId")]
        public string? PaymentId { get; set; }
    }
}
