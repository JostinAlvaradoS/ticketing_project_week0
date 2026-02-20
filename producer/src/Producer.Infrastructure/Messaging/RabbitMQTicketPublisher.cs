using System.Text.Json;
using Producer.Application.Ports.Outbound;
using Producer.Domain.Events;
using RabbitMQ.Client;

namespace Producer.Infrastructure.Messaging;

public class RabbitMQTicketPublisher : ITicketEventPublisher
{
    private readonly IConnection _connection;
    private readonly RabbitMQSettings _settings;

    public RabbitMQTicketPublisher(IConnection connection, RabbitMQSettings settings)
    {
        _connection = connection;
        _settings = settings;
    }

    public async Task PublishAsync(TicketReservedEvent ticketEvent, CancellationToken cancellationToken = default)
    {
        using var channel = _connection.CreateModel();

        var json = JsonSerializer.Serialize(ticketEvent);
        var body = System.Text.Encoding.UTF8.GetBytes(json);

        var properties = channel.CreateBasicProperties();
        properties.ContentType = "application/json";
        properties.DeliveryMode = 2;
        properties.Timestamp = new AmqpTimestamp(DateTimeOffset.UtcNow.ToUnixTimeSeconds());

        channel.BasicPublish(
            exchange: _settings.ExchangeName,
            routingKey: _settings.TicketReservedRoutingKey,
            basicProperties: properties,
            body: body);

        await Task.CompletedTask;
    }
}
