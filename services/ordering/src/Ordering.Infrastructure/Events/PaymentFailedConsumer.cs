using System.Text.Json;
using System.Text.Json.Serialization;
using Confluent.Kafka;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Ordering.Application.Ports;

namespace Ordering.Infrastructure.Events;

/// <summary>
/// Consumes `payment-failed` events and cancels the associated order.
/// Prevents orders stuck in pending state when payment is declined.
/// </summary>
public class PaymentFailedConsumer : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<PaymentFailedConsumer> _logger;
    private readonly KafkaOptions _kafkaOptions;

    public PaymentFailedConsumer(
        IServiceProvider serviceProvider,
        ILogger<PaymentFailedConsumer> logger,
        IOptions<KafkaOptions> kafkaOptions)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _kafkaOptions = kafkaOptions.Value;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_kafkaOptions.EnableConsumer)
        {
            _logger.LogInformation("Kafka consumer is disabled, skipping payment-failed consumption");
            return;
        }

        await Task.Delay(2000, stoppingToken); // allow Kafka to be ready

        var config = new ConsumerConfig
        {
            BootstrapServers = _kafkaOptions.BootstrapServers,
            GroupId = "ordering-payment-failed-consumer",
            AutoOffsetReset = AutoOffsetReset.Earliest,
            EnableAutoCommit = true
        };

        using var consumer = new ConsumerBuilder<string, string>(config)
            .SetErrorHandler((_, e) => _logger.LogError("Kafka error: {Error}", e.Reason))
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
        PaymentFailedMessage? evt;
        try
        {
            evt = JsonSerializer.Deserialize<PaymentFailedMessage>(messageJson,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to deserialize payment-failed event");
            return;
        }

        if (evt == null || !Guid.TryParse(evt.OrderId, out var orderId))
        {
            _logger.LogWarning("payment-failed event missing or invalid orderId, skipping");
            return;
        }

        using var scope = _serviceProvider.CreateScope();
        var orderRepo = scope.ServiceProvider.GetRequiredService<IOrderRepository>();

        var order = await orderRepo.GetByIdAsync(orderId, cancellationToken);
        if (order == null)
        {
            _logger.LogWarning("Order {OrderId} not found for payment-failed event, skipping", orderId);
            return;
        }

        // Idempotency: skip if already in terminal state
        if (order.State == Domain.Entities.Order.StateCancelled || order.State == Domain.Entities.Order.StatePaid)
        {
            _logger.LogInformation("Order {OrderId} already in state {State}, skipping", orderId, order.State);
            return;
        }

        try
        {
            order.Cancel();
            await orderRepo.UpdateAsync(order, cancellationToken);
            _logger.LogInformation("Order {OrderId} cancelled due to payment-failed event", orderId);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning("Could not cancel order {OrderId}: {Reason}", orderId, ex.Message);
        }
    }

    private sealed class PaymentFailedMessage
    {
        [JsonPropertyName("orderId")]
        public string? OrderId { get; init; }
    }
}
