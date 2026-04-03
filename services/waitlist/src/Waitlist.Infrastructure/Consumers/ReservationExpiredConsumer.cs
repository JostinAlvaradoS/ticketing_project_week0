using System.Text.Json;
using System.Text.Json.Serialization;
using Confluent.Kafka;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Waitlist.Application.UseCases.AssignNext;

namespace Waitlist.Infrastructure.Consumers;

/// <summary>
/// Consumes `reservation-expired` events (v3 payload) and dispatches AssignNextCommand.
/// v3 payload includes: messageId, concertEventId, seatId.
/// </summary>
public class ReservationExpiredConsumer : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ReservationExpiredConsumer> _logger;
    private readonly string _bootstrapServers;

    public ReservationExpiredConsumer(
        IServiceScopeFactory scopeFactory,
        ILogger<ReservationExpiredConsumer> logger,
        string bootstrapServers)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _bootstrapServers = bootstrapServers;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Yield();

        var config = new ConsumerConfig
        {
            BootstrapServers = _bootstrapServers,
            GroupId = "waitlist-reservation-expired-consumer",
            AutoOffsetReset = AutoOffsetReset.Earliest,
            EnableAutoCommit = true
        };

        using var consumer = new ConsumerBuilder<string?, string>(config)
            .SetErrorHandler((_, e) => _logger.LogError("Kafka consumer error: {Error}", e.Reason))
            .Build();

        consumer.Subscribe("reservation-expired");
        _logger.LogInformation("ReservationExpiredConsumer subscribed to topic reservation-expired");

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var result = consumer.Consume(TimeSpan.FromMilliseconds(500));
                    if (result?.Message?.Value != null)
                        await ProcessMessageAsync(result.Message.Value, stoppingToken);
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
        }
    }

    public async Task ProcessMessageAsync(string messageJson, CancellationToken cancellationToken)
    {
        ReservationExpiredV3Event? evt;
        try
        {
            evt = JsonSerializer.Deserialize<ReservationExpiredV3Event>(messageJson,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to deserialize reservation-expired event");
            return;
        }

        if (evt?.ConcertEventId is null || evt.SeatId is null)
        {
            _logger.LogWarning("reservation-expired v3 event missing concertEventId or seatId, skipping");
            return;
        }

        if (!Guid.TryParse(evt.ConcertEventId, out var eventId) ||
            !Guid.TryParse(evt.SeatId, out var seatId))
        {
            _logger.LogWarning("reservation-expired v3 event has invalid GUIDs, skipping");
            return;
        }

        using var scope = _scopeFactory.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        await mediator.Send(new AssignNextCommand(eventId, seatId), cancellationToken);

        _logger.LogInformation("Dispatched AssignNextCommand for eventId={EventId} seatId={SeatId}", eventId, seatId);
    }

    private sealed class ReservationExpiredV3Event
    {
        [JsonPropertyName("messageId")]
        public string? MessageId { get; init; }

        [JsonPropertyName("concertEventId")]
        public string? ConcertEventId { get; init; }

        [JsonPropertyName("seatId")]
        public string? SeatId { get; init; }
    }
}
