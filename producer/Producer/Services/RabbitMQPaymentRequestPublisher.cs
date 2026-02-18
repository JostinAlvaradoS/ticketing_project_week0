using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using Producer.Configurations;
using Producer.Models;
using RabbitMQ.Client;

namespace Producer.Services;

public class RabbitMQPaymentRequestPublisher : IPaymentRequestPublisher
{
    private readonly IConnection _connection;
    private readonly RabbitMQOptions _options;
    private readonly ILogger<RabbitMQPaymentRequestPublisher> _logger;

    public RabbitMQPaymentRequestPublisher(
        IConnection connection,
        IOptions<RabbitMQOptions> options,
        ILogger<RabbitMQPaymentRequestPublisher> logger)
    {
        _connection = connection;
        _options = options.Value;
        _logger = logger;
    }

    public async Task PublishPaymentRequestedAsync(PaymentRequestedEvent paymentEvent, CancellationToken cancellationToken = default)
    {
        using var channel = _connection.CreateModel();

        var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(paymentEvent));

        var properties = channel.CreateBasicProperties();
        properties.Persistent = true;
        properties.ContentType = "application/json";
        properties.Timestamp = new AmqpTimestamp(DateTimeOffset.UtcNow.ToUnixTimeSeconds());

        channel.BasicPublish(
            exchange: _options.ExchangeName,
            routingKey: _options.PaymentRequestedRoutingKey,
            basicProperties: properties,
            body: body);

        _logger.LogInformation(
            "Payment request published. TicketId={TicketId}, EventId={EventId}",
            paymentEvent.TicketId,
            paymentEvent.EventId);

        await Task.CompletedTask;
    }
}
