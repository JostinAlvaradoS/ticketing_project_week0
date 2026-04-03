// Consumer: payment-succeeded → CompleteAssignment

using System.Text.Json;
using System.Text.Json.Serialization;
using Confluent.Kafka;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Waitlist.Application.UseCases.CompleteAssignment;
using Waitlist.Infrastructure.Options;

namespace Waitlist.Infrastructure.Consumers;

public class PaymentSucceededConsumer : BackgroundService
{
    private readonly IServiceProvider  _services;
    private readonly KafkaOptions      _kafkaOptions;
    private readonly ILogger<PaymentSucceededConsumer> _logger;

    public PaymentSucceededConsumer(
        IServiceProvider                    services,
        IOptions<KafkaOptions>              kafkaOptions,
        ILogger<PaymentSucceededConsumer>   logger)
    {
        _services     = services;
        _kafkaOptions = kafkaOptions.Value;
        _logger       = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Yield immediately so the host can finish startup before this blocks on Consume()
        await Task.Yield();

        var config = new ConsumerConfig
        {
            BootstrapServers = _kafkaOptions.BootstrapServers,
            GroupId          = "waitlist-payment-succeeded",
            AutoOffsetReset  = AutoOffsetReset.Earliest,
            EnableAutoCommit = false
        };

        using var consumer = new ConsumerBuilder<Ignore, string>(config).Build();
        consumer.Subscribe("payment-succeeded");

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
                _logger.LogError(ex, "Error processing payment-succeeded message");
            }
        }

        consumer.Close();
    }

    public static async Task ProcessMessageAsync(string json, IMediator mediator, CancellationToken cancellationToken)
    {
        var evt = JsonSerializer.Deserialize<PaymentSucceededEvent>(json,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        if (evt is null || evt.OrderId == Guid.Empty) return;

        await mediator.Send(new CompleteAssignmentCommand(evt.OrderId), cancellationToken);
    }

    private sealed class PaymentSucceededEvent
    {
        [JsonPropertyName("orderId")]
        public Guid OrderId { get; init; }
    }
}
