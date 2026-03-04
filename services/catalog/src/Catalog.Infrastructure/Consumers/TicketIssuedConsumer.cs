using System.Text.Json;
using Confluent.Kafka;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Catalog.Application.Ports;

namespace Catalog.Infrastructure.Consumers;

/// <summary>
/// Background worker that consumes ticket-issued events from Kafka
/// and marks the corresponding catalog seat as "sold".
/// </summary>
public class TicketIssuedConsumer : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IConsumer<string?, string> _consumer;

    public TicketIssuedConsumer(IServiceScopeFactory scopeFactory, IConsumer<string?, string> consumer)
    {
        _scopeFactory = scopeFactory;
        _consumer = consumer;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Yield();

        _consumer.Subscribe("ticket-issued");
        Console.WriteLine("[TicketIssuedConsumer] Subscribed to 'ticket-issued' topic");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var result = _consumer.Consume(stoppingToken);

                if (result?.Message?.Value == null)
                    continue;

                Console.WriteLine($"[TicketIssuedConsumer] Received message: key={result.Message.Key}");

                await ProcessMessage(result.Message.Value, stoppingToken);

                _consumer.Commit(result);
            }
            catch (ConsumeException ex)
            {
                Console.WriteLine($"[TicketIssuedConsumer] Consume error: {ex.Error.Reason}");
                await Task.Delay(1000, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[TicketIssuedConsumer] Error processing message: {ex.Message}");
                await Task.Delay(1000, stoppingToken);
            }
        }

        _consumer.Close();
    }

    private async Task ProcessMessage(string messageJson, CancellationToken cancellationToken)
    {
        var ticketEvent = JsonSerializer.Deserialize<TicketIssuedEvent>(messageJson, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        if (ticketEvent == null || ticketEvent.SeatId == Guid.Empty)
        {
            Console.WriteLine("[TicketIssuedConsumer] Invalid event, skipping");
            return;
        }

        using var scope = _scopeFactory.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<ICatalogRepository>();

        var seat = await repository.GetSeatAsync(ticketEvent.SeatId, cancellationToken);
        if (seat == null)
        {
            Console.WriteLine($"[TicketIssuedConsumer] Seat {ticketEvent.SeatId} not found in catalog, skipping");
            return;
        }

        if (seat.IsSold())
        {
            Console.WriteLine($"[TicketIssuedConsumer] Seat {ticketEvent.SeatId} already sold, skipping");
            return;
        }

        seat.Sell();
        await repository.SaveChangesAsync(cancellationToken);
        Console.WriteLine($"[TicketIssuedConsumer] Seat {ticketEvent.SeatId} marked as sold for event {ticketEvent.EventId}");
    }

    private class TicketIssuedEvent
    {
        public Guid TicketId { get; set; }
        public Guid OrderId { get; set; }
        public Guid EventId { get; set; }
        public Guid SeatId { get; set; }
        public string SeatNumber { get; set; } = string.Empty;
        public string Section { get; set; } = string.Empty;
    }
}
