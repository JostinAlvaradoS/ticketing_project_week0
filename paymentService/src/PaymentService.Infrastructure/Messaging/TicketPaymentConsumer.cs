using System.Text;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace PaymentService.Infrastructure.Messaging;

public class TicketPaymentConsumer : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly RabbitMQSettings _settings;
    private readonly ILogger<TicketPaymentConsumer> _logger;

    private IConnection? _connection;
    private IChannel? _channel;

    public TicketPaymentConsumer(
        IServiceScopeFactory scopeFactory,
        IOptions<RabbitMQSettings> settings,
        ILogger<TicketPaymentConsumer> logger)
    {
        _scopeFactory = scopeFactory;
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

        _logger.LogInformation(
            "Connected to RabbitMQ. Listening on queues: {ApprovedQueue}, {RejectedQueue}",
            _settings.ApprovedQueueName, _settings.RejectedQueueName);

        // Consumer for approved payments
        await ConsumeQueueAsync<PaymentApprovedEventHandler>(_settings.ApprovedQueueName, stoppingToken);

        // Consumer for rejected payments
        await ConsumeQueueAsync<PaymentRejectedEventHandler>(_settings.RejectedQueueName, stoppingToken);

        _logger.LogInformation("Consumer started, waiting for messages...");

        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(1000, stoppingToken);
        }
    }

    private async Task ConsumeQueueAsync<THandler>(string queueName, CancellationToken stoppingToken)
        where THandler : IPaymentEventHandler
    {
        var consumer = new AsyncEventingBasicConsumer(_channel!);

        consumer.ReceivedAsync += async (_, eventArgs) =>
        {
            var bodyBytes = eventArgs.Body.ToArray();
            var json = Encoding.UTF8
                .GetString(bodyBytes)
                .Replace("\u00A0", " ")
                .Trim();
            _logger.LogInformation("Message received from queue {Queue}: {Json}", queueName, json);

            try
            {
                using var scope = _scopeFactory.CreateScope();
                var handler = scope.ServiceProvider.GetRequiredService<THandler>();
                var result = await handler.HandleAsync(json, stoppingToken);

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
                    _logger.LogWarning("Message processing failed: {Reason}. Discarding message (ACK).", result.FailureReason);
                    await _channel!.BasicAckAsync(eventArgs.DeliveryTag, multiple: false, stoppingToken);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing message from queue {Queue}. Discarding message (NACK without requeue).", queueName);
                await _channel!.BasicNackAsync(eventArgs.DeliveryTag, multiple: false, requeue: false, cancellationToken: stoppingToken);
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
