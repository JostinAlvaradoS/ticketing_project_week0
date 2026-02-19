using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PaymentService.Application.Dtos;
using PaymentService.Application.Ports.Inbound;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace PaymentService.Infrastructure.Messaging;

public class RabbitMQSettings
{
    public string HostName { get; set; } = "localhost";
    public string UserName { get; set; } = "guest";
    public string Password { get; set; } = "guest";
    public string ApprovedQueueName { get; set; } = "payment-approved-queue";
    public string RejectedQueueName { get; set; } = "payment-rejected-queue";
    public string ExchangeName { get; set; } = "payment-events";
}

public interface IPaymentEventHandler
{
    string QueueName { get; }
    Task<ValidationResult> HandleAsync(string json, CancellationToken cancellationToken = default);
}

public class PaymentApprovedEventHandler : IPaymentEventHandler
{
    private readonly IProcessPaymentApprovedUseCase _useCase;
    private readonly RabbitMQSettings _settings;
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public PaymentApprovedEventHandler(
        IProcessPaymentApprovedUseCase useCase,
        IOptions<RabbitMQSettings> settings)
    {
        _useCase = useCase;
        _settings = settings.Value;
    }

    public string QueueName => _settings.ApprovedQueueName;

    public async Task<ValidationResult> HandleAsync(string json, CancellationToken cancellationToken = default)
    {
        var evt = JsonSerializer.Deserialize<PaymentApprovedEventDto>(json, JsonOptions);
        if (evt == null)
            return ValidationResult.Failure("Invalid JSON for PaymentApprovedEvent");

        return await _useCase.ExecuteAsync(evt);
    }
}

public class PaymentRejectedEventHandler : IPaymentEventHandler
{
    private readonly IProcessPaymentRejectedUseCase _useCase;
    private readonly RabbitMQSettings _settings;
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public PaymentRejectedEventHandler(
        IProcessPaymentRejectedUseCase useCase,
        IOptions<RabbitMQSettings> settings)
    {
        _useCase = useCase;
        _settings = settings.Value;
    }

    public string QueueName => _settings.RejectedQueueName;

    public async Task<ValidationResult> HandleAsync(string json, CancellationToken cancellationToken = default)
    {
        var evt = JsonSerializer.Deserialize<PaymentRejectedEventDto>(json, JsonOptions);
        if (evt == null)
            return ValidationResult.Failure("Invalid JSON for PaymentRejectedEvent");

        return await _useCase.ExecuteAsync(evt);
    }
}

public class TicketPaymentConsumer : BackgroundService
{
    private readonly ILogger<TicketPaymentConsumer> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly RabbitMQSettings _settings;
    private IConnection? _connection;
    private IModel? _channel;

    public TicketPaymentConsumer(
        ILogger<TicketPaymentConsumer> logger,
        IOptions<RabbitMQSettings> settings,
        IServiceProvider serviceProvider)
    {
        _logger = logger;
        _settings = settings.Value;
        _serviceProvider = serviceProvider;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Yield();

        try
        {
            var factory = new ConnectionFactory
            {
                HostName = _settings.HostName,
                UserName = _settings.UserName,
                Password = _settings.Password
            };

            _connection = factory.CreateConnection();
            _channel = _connection.CreateModel();

            _channel.ExchangeDeclare(_settings.ExchangeName, ExchangeType.Direct, durable: true);

            var handlerTypes = new[] { typeof(PaymentApprovedEventHandler), typeof(PaymentRejectedEventHandler) };

            foreach (var handlerType in handlerTypes)
            {
                _channel.QueueDeclare(handlerType.Name.Replace("EventHandler", "-queue"), durable: true, exclusive: false, autoDelete: false);
                _channel.QueueBind(handlerType.Name.Replace("EventHandler", "-queue"), _settings.ExchangeName, handlerType.Name.Replace("EventHandler", ""));
            }

            foreach (var handlerType in handlerTypes)
            {
                var consumer = new EventingBasicConsumer(_channel);
                var queueName = handlerType.Name.Replace("EventHandler", "-queue");
                var routingKey = handlerType.Name.Replace("EventHandler", "");

                consumer.Received += async (model, ea) =>
                {
                    var body = ea.Body.ToArray();
                    var json = System.Text.Encoding.UTF8.GetString(body);

                    _logger.LogInformation("Received message from queue {Queue}", queueName);

                    try
                    {
                        using var scope = _serviceProvider.CreateScope();
                        var handler = scope.ServiceProvider.GetRequiredService(handlerType) as IPaymentEventHandler;
                        var result = await handler!.HandleAsync(json, stoppingToken);

                        if (result.IsSuccess || result.IsAlreadyProcessed)
                        {
                            _channel.BasicAck(ea.DeliveryTag, false);
                            _logger.LogInformation("Message processed successfully");
                        }
                        else
                        {
                            _logger.LogWarning("Message processing failed: {Reason}", result.FailureReason);
                            _channel.BasicNack(ea.DeliveryTag, false, true);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error processing message");
                        _channel.BasicNack(ea.DeliveryTag, false, true);
                    }
                };

                _channel.BasicConsume(queue: queueName, autoAck: false, consumer: consumer);
            }

            _logger.LogInformation("Consumer started, waiting for messages...");

            while (!stoppingToken.IsCancellationRequested)
            {
                await Task.Delay(1000, stoppingToken);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in RabbitMQ consumer");
        }
    }

    public override void Dispose()
    {
        _channel?.Close();
        _connection?.Close();
        base.Dispose();
    }
}
