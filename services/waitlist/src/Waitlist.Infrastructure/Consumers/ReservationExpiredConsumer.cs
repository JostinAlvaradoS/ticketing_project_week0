// TDD Ciclo 15 — GREEN: consumer Kafka reservation-expired v3

using System.Text.Json;
using System.Text.Json.Serialization;
using Confluent.Kafka;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Waitlist.Application.UseCases.AssignNext;
using Waitlist.Infrastructure.Options;

namespace Waitlist.Infrastructure.Consumers;

public class ReservationExpiredConsumer : BackgroundService
{
    private readonly IServiceProvider   _services;
    private readonly KafkaOptions       _kafkaOptions;
    private readonly ILogger<ReservationExpiredConsumer> _logger;

    public ReservationExpiredConsumer(
        IServiceProvider                     services,
        IOptions<KafkaOptions>               kafkaOptions,
        ILogger<ReservationExpiredConsumer>  logger)
    {
        _services     = services;
        _kafkaOptions = kafkaOptions.Value;
        _logger       = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var config = new ConsumerConfig
        {
            BootstrapServers = _kafkaOptions.BootstrapServers,
            GroupId          = "waitlist-reservation-expired",
            AutoOffsetReset  = AutoOffsetReset.Earliest,
            EnableAutoCommit = false
        };

        using var consumer = new ConsumerBuilder<Ignore, string>(config).Build();
        consumer.Subscribe("reservation-expired");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var result = consumer.Consume(TimeSpan.FromSeconds(1));
                if (result?.Message?.Value is null) continue;

                using var scope    = _services.CreateScope();
                var       mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

                await ProcessMessageAsync(result.Message.Value, mediator, stoppingToken);
                consumer.Commit(result);
            }
            catch (OperationCanceledException) { break; }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing reservation-expired message");
            }
        }

        consumer.Close();
    }

    // ── Static helper — testable without Kafka infrastructure ─────
    public static async Task ProcessMessageAsync(string json, IMediator mediator, CancellationToken cancellationToken)
    {
        var evt = JsonSerializer.Deserialize<ReservationExpiredEventV3>(json,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        if (evt is null) return;

        // Guard: concertEventId debe ser válido (campo nuevo en v3)
        if (evt.ConcertEventId == Guid.Empty) return;
        if (evt.SeatId == Guid.Empty) return;

        await mediator.Send(new AssignNextCommand(evt.SeatId, evt.ConcertEventId), cancellationToken);
    }

    private sealed class ReservationExpiredEventV3
    {
        [JsonPropertyName("messageId")]
        public Guid MessageId { get; init; }

        [JsonPropertyName("reservationId")]
        public Guid ReservationId { get; init; }

        [JsonPropertyName("seatId")]
        public Guid SeatId { get; init; }

        [JsonPropertyName("customerId")]
        public string? CustomerId { get; init; }

        [JsonPropertyName("concertEventId")]
        public Guid ConcertEventId { get; init; }
    }
}
