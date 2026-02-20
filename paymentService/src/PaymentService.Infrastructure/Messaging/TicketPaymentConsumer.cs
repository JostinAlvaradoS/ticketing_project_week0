using System.Text;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace PaymentService.Infrastructure.Messaging;

/// <summary>
/// RabbitMQ consumer that uses the Strategy pattern to dispatch messages
/// to the correct handler based on queue-to-event-type mappings.
/// Adding a new event type does NOT require modifying this class (OCP).
/// </summary>
public class TicketPaymentConsumer : BackgroundService
{
    private readonly PaymentEventStrategyResolver _strategyResolver;
    private readonly RabbitMQSettings _settings;
    private readonly ILogger<TicketPaymentConsumer> _logger;

    private IConnection? _connection;
    private IChannel? _channel;

    public TicketPaymentConsumer(
        PaymentEventStrategyResolver strategyResolver,
        IOptions<RabbitMQSettings> settings,
        ILogger<TicketPaymentConsumer> logger)
    {
        _strategyResolver = strategyResolver;
        _settings = settings.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var factory = new ConnectionFactory
        {
            HostName = _settings.Host,
            Port = _settings.Port,
            UserName = _settings.Username,
            Password = _settings.Password
        };

        _connection = await factory.CreateConnectionAsync(stoppingToken);
        _channel = await _connection.CreateChannelAsync(cancellationToken: stoppingToken);

        var queueMappings = _settings.GetQueueEventTypeMappings();

        _logger.LogInformation(
            "Connected to RabbitMQ. Listening on queues: {Queues}",
            string.Join(", ", queueMappings.Keys));

        // Subscribe to each queue with its mapped event type
        foreach (var (queueName, eventType) in queueMappings)
        {
            await ConsumeQueueAsync(queueName, eventType, stoppingToken);
        }

        _logger.LogInformation("Consumer started, waiting for messages...");

        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(1000, stoppingToken);
        }
    }

    private async Task ConsumeQueueAsync(string queueName, string eventType, CancellationToken stoppingToken)
    {
        var consumer = new AsyncEventingBasicConsumer(_channel!);

        consumer.ReceivedAsync += async (_, eventArgs) =>
        {
            var bodyBytes = eventArgs.Body.ToArray();
            var json = Encoding.UTF8
                .GetString(bodyBytes)
                .Replace("\u00A0", " ")
                .Trim();
            _logger.LogInformation("Message received from queue {Queue} (eventType={EventType}): {Json}",
                queueName, eventType, json);

            try
            {
                var (strategy, scope) = _strategyResolver.Resolve(eventType);
                using (scope)
                {
                    var result = await strategy.HandleAsync(json, stoppingToken);

                    _logger.LogInformation(
                        "Result: IsSuccess={IsSuccess}, FailureReason={FailureReason}",
                        result.IsSuccess, result.FailureReason);

                    if (result.IsSuccess || result.IsAlreadyProcessed)
                    {
                        await _channel!.BasicAckAsync(eventArgs.DeliveryTag, multiple: false, stoppingToken);
                        _logger.LogInformation("Message processed successfully");
                    }
                    else
                    {
                        _logger.LogWarning("Message processing failed: {Reason}. Discarding message (ACK).",
                            result.FailureReason);
                        await _channel!.BasicAckAsync(eventArgs.DeliveryTag, multiple: false, stoppingToken);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Error processing message from queue {Queue}. Discarding message (NACK without requeue).",
                    queueName);
                await _channel!.BasicNackAsync(eventArgs.DeliveryTag, multiple: false, requeue: false,
                    cancellationToken: stoppingToken);
            }
        };

        await _channel!.BasicConsumeAsync(queueName, autoAck: false, consumer, stoppingToken);
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Stopping consumer...");

        if (_channel is not null) await _channel.CloseAsync(cancellationToken);
        if (_connection is not null) await _connection.CloseAsync(cancellationToken);

        await base.StopAsync(cancellationToken);
    }
}
