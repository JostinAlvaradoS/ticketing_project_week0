using System.Text;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client.Events;
using RabbitMQ.Client;
using MsPaymentService.Worker.Models.Events;

namespace MsPaymentService.Worker.Messaging;

public class TicketPaymentConsumer
{
    private readonly RabbitMQConnection _connection;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<TicketPaymentConsumer> _logger;

    public TicketPaymentConsumer(
        RabbitMQConnection connection,
        IServiceScopeFactory scopeFactory,
        ILogger<TicketPaymentConsumer> logger)
    {
        _connection = connection;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public void Start()
    {
        var channel = _connection.GetChannel();

        channel.BasicQos(0, 1, false); // consumo seguro

        var consumer = new AsyncEventingBasicConsumer(channel);
        consumer.Received += OnMessageReceivedAsync;

        channel.BasicConsume(
            queue: "payment.reservation.consumer", // TU cola
            autoAck: false,
            consumer: consumer
        );

        _logger.LogInformation("TicketPaymentConsumer iniciado");
    }

    private async Task OnMessageReceivedAsync(object sender, BasicDeliverEventArgs args)
    {
        var channel = _connection.GetChannel();

        try
        {
            var json = Encoding.UTF8.GetString(args.Body.ToArray());
            var evt = JsonSerializer.Deserialize<TicketPaymentEvent>(json);

            if (evt == null)
            {
                _logger.LogError("Evento inválido (null)");
                channel.BasicAck(args.DeliveryTag, false);
                return;
            }

            _logger.LogInformation(
                "Evento recibido EventId={EventId}, TicketId={TicketId}",
                evt.EventId, evt.TicketId
            );

            // 👉 AQUÍ TU LÓGICA MÍNIMA
            // Validar ticket, crear intención de pago, etc.
            await Task.CompletedTask;

            channel.BasicAck(args.DeliveryTag, false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error procesando evento");

            // MVP: no requeue infinito
            channel.BasicNack(
                deliveryTag: args.DeliveryTag,
                multiple: false,
                requeue: false // va a DLQ
            );
        }
    }
}
