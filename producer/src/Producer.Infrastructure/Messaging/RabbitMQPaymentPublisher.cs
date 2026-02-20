using System.Text;
using System.Text.Json;
using Producer.Application.Ports.Outbound;
using Producer.Domain.Events;
using RabbitMQ.Client;

namespace Producer.Infrastructure.Messaging;

public class RabbitMQPaymentPublisher : IPaymentEventPublisher
{
    private readonly IConnection _connection;
    private readonly RabbitMQSettings _settings;

    public RabbitMQPaymentPublisher(IConnection connection, RabbitMQSettings settings)
    {
        _connection = connection;
        _settings = settings;
    }

    public async Task PublishApprovedAsync(PaymentApprovedEvent paymentEvent, CancellationToken cancellationToken = default)
    {
        if (paymentEvent == null)
            throw new ArgumentNullException(nameof(paymentEvent));

        using var channel = _connection.CreateModel();

        channel.ExchangeDeclare(
            exchange: _settings.ExchangeName,
            type: ExchangeType.Topic,
            durable: true,
            autoDelete: false);

        var message = JsonSerializer.Serialize(paymentEvent);
        var body = Encoding.UTF8.GetBytes(message);

        var properties = channel.CreateBasicProperties();
        properties.Persistent = true;
        properties.ContentType = "application/json";
        properties.Timestamp = new AmqpTimestamp(DateTimeOffset.UtcNow.ToUnixTimeSeconds());

        channel.BasicPublish(
            exchange: _settings.ExchangeName,
            routingKey: _settings.PaymentApprovedRoutingKey,
            mandatory: false,
            basicProperties: properties,
            body: body);

        await Task.CompletedTask;
    }

    public async Task PublishRejectedAsync(PaymentRejectedEvent paymentEvent, CancellationToken cancellationToken = default)
    {
        if (paymentEvent == null)
            throw new ArgumentNullException(nameof(paymentEvent));

        using var channel = _connection.CreateModel();

        channel.ExchangeDeclare(
            exchange: _settings.ExchangeName,
            type: ExchangeType.Topic,
            durable: true,
            autoDelete: false);

        var message = JsonSerializer.Serialize(paymentEvent);
        var body = Encoding.UTF8.GetBytes(message);

        var properties = channel.CreateBasicProperties();
        properties.Persistent = true;
        properties.ContentType = "application/json";
        properties.Timestamp = new AmqpTimestamp(DateTimeOffset.UtcNow.ToUnixTimeSeconds());

        channel.BasicPublish(
            exchange: _settings.ExchangeName,
            routingKey: _settings.PaymentRejectedRoutingKey,
            mandatory: false,
            basicProperties: properties,
            body: body);

        await Task.CompletedTask;
    }
}
