using System.Text.Json;
using Confluent.Kafka;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Inventory.Domain.Entities;
using Inventory.Infrastructure.Persistence;

namespace Inventory.Infrastructure.Consumers;

/// <summary>
/// Background worker that consumes seats-generated events from Kafka
/// and inserts the seats into the inventory database (bc_inventory."Seats").
/// </summary>
public class SeatsGeneratedConsumer : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IConsumer<string?, string> _consumer;

    public SeatsGeneratedConsumer(IServiceScopeFactory scopeFactory, IConsumer<string?, string> consumer)
    {
        _scopeFactory = scopeFactory;
        _consumer = consumer;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Yield to let the host start before blocking on Consume
        await Task.Yield();

        _consumer.Subscribe("seats-generated");
        Console.WriteLine("[SeatsGeneratedConsumer] Subscribed to 'seats-generated' topic");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var result = _consumer.Consume(stoppingToken);

                if (result?.Message?.Value == null)
                    continue;

                Console.WriteLine($"[SeatsGeneratedConsumer] Received message: key={result.Message.Key}");

                await ProcessMessage(result.Message.Value, stoppingToken);

                _consumer.Commit(result);
            }
            catch (ConsumeException ex)
            {
                Console.WriteLine($"[SeatsGeneratedConsumer] Consume error: {ex.Error.Reason}");
                await Task.Delay(1000, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[SeatsGeneratedConsumer] Error processing message: {ex.Message}");
                await Task.Delay(1000, stoppingToken);
            }
        }

        _consumer.Close();
    }

    private async Task ProcessMessage(string messageJson, CancellationToken cancellationToken)
    {
        var seatsEvent = JsonSerializer.Deserialize<SeatsGeneratedEvent>(messageJson, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        if (seatsEvent?.Seats == null || seatsEvent.Seats.Count == 0)
        {
            Console.WriteLine("[SeatsGeneratedConsumer] Empty or null seats list, skipping");
            return;
        }

        using var scope = _scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<InventoryDbContext>();

        var inventorySeats = new List<Seat>();

        foreach (var seatDto in seatsEvent.Seats)
        {
            // Check if seat already exists (idempotency)
            var exists = await dbContext.Seats.FindAsync(new object[] { seatDto.SeatId }, cancellationToken);
            if (exists != null)
            {
                Console.WriteLine($"[SeatsGeneratedConsumer] Seat {seatDto.SeatId} already exists, skipping");
                continue;
            }

            inventorySeats.Add(new Seat
            {
                Id = seatDto.SeatId,
                Section = seatDto.Section,
                Row = seatDto.Row,
                Number = seatDto.Number,
                Reserved = false
            });
        }

        if (inventorySeats.Count > 0)
        {
            await dbContext.Seats.AddRangeAsync(inventorySeats, cancellationToken);
            await dbContext.SaveChangesAsync(cancellationToken);
            Console.WriteLine($"[SeatsGeneratedConsumer] Inserted {inventorySeats.Count} seats for event {seatsEvent.EventId}");
        }
    }

    /// <summary>
    /// DTO matching the seats-generated Kafka contract.
    /// </summary>
    private class SeatsGeneratedEvent
    {
        public Guid EventId { get; set; }
        public List<SeatDto> Seats { get; set; } = new();
        public int TotalSeats { get; set; }
        public DateTime GeneratedAt { get; set; }
    }

    private class SeatDto
    {
        public Guid SeatId { get; set; }
        public string Section { get; set; } = string.Empty;
        public string Row { get; set; } = string.Empty;
        public int Number { get; set; }
    }
}
