using System.Text.Json;
using System.Text.Json.Serialization;
using Confluent.Kafka;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Waitlist.Application.UseCases.CompleteAssignment;

namespace Waitlist.Infrastructure.Consumers;

/// <summary>
/// Consumes `payment-succeeded` events and completes the waitlist assignment.
/// </summary>
public class PaymentSucceededConsumer : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<PaymentSucceededConsumer> _logger;
    private readonly string _bootstrapServers;

    public PaymentSucceededConsumer(
        IServiceScopeFactory scopeFactory,
        ILogger<PaymentSucceededConsumer> logger,
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
            GroupId = "waitlist-payment-succeeded-consumer",
            AutoOffsetReset = AutoOffsetReset.Earliest,
            EnableAutoCommit = true
        };

        using var consumer = new ConsumerBuilder<string?, string>(config)
            .SetErrorHandler((_, e) => _logger.LogError("Kafka consumer error: {Error}", e.Reason))
            .Build();

        consumer.Subscribe("payment-succeeded");
        _logger.LogInformation("PaymentSucceededConsumer subscribed to topic payment-succeeded");

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
                    _logger.LogError(ex, "Error consuming payment-succeeded message");
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Unexpected error in PaymentSucceededConsumer");
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
        PaymentSucceededEvent? evt;
        try
        {
            evt = JsonSerializer.Deserialize<PaymentSucceededEvent>(messageJson,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to deserialize payment-succeeded event");
            return;
        }

        if (evt?.WaitlistEntryId is null)
        {
            _logger.LogDebug("payment-succeeded event has no waitlistEntryId, skipping waitlist processing");
            return;
        }

        if (!Guid.TryParse(evt.WaitlistEntryId, out var entryId))
        {
            _logger.LogWarning("payment-succeeded event has invalid waitlistEntryId, skipping");
            return;
        }

        using var scope = _scopeFactory.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        await mediator.Send(new CompleteAssignmentCommand(entryId), cancellationToken);

        _logger.LogInformation("Dispatched CompleteAssignmentCommand for entryId={EntryId}", entryId);
    }

    private sealed class PaymentSucceededEvent
    {
        [JsonPropertyName("waitlistEntryId")]
        public string? WaitlistEntryId { get; init; }

        [JsonPropertyName("orderId")]
        public string? OrderId { get; init; }
    }
}
