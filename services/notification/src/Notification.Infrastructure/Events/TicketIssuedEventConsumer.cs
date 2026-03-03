using Confluent.Kafka;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Notification.Application.UseCases.SendTicketNotification;
using System.Text.Json;

namespace Notification.Infrastructure.Events;

public class TicketIssuedEventConsumer : BackgroundService
{
    private readonly IConsumer<string, string> _consumer;
    private readonly KafkaOptions _kafkaOptions;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<TicketIssuedEventConsumer> _logger;
    private readonly string _topicName;

    public TicketIssuedEventConsumer(
        IOptions<KafkaOptions> kafkaOptions,
        IServiceProvider serviceProvider,
        ILogger<TicketIssuedEventConsumer> logger)
    {
        _kafkaOptions = kafkaOptions.Value;
        _serviceProvider = serviceProvider;
        _logger = logger;

        var config = new ConsumerConfig
        {
            BootstrapServers = _kafkaOptions.BootstrapServers,
            GroupId = _kafkaOptions.ConsumerGroupId,
            AutoOffsetReset = AutoOffsetReset.Earliest,
            EnableAutoCommit = true,
        };

        _consumer = new ConsumerBuilder<string, string>(config).Build();
        _topicName = _kafkaOptions.Topics.TryGetValue("TicketIssued", out var topic)
            ? topic
            : "ticket-issued";
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("TicketIssuedEventConsumer starting...");
        _consumer.Subscribe(_topicName);

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                var consumeResult = _consumer.Consume(TimeSpan.FromSeconds(10));

                if (consumeResult == null)
                    continue;

                try
                {
                    var ticketEvent = JsonSerializer.Deserialize<TicketIssuedEvent>(
                        consumeResult.Message.Value,
                        new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });

                    if (ticketEvent == null)
                    {
                        _logger.LogWarning("Failed to deserialize ticket-issued event");
                        continue;
                    }

                    // Process the event using MediatR
                    using (var scope = _serviceProvider.CreateScope())
                    {
                        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

                        var command = new SendTicketNotificationCommand
                        {
                            TicketId = ticketEvent.TicketId,
                            OrderId = ticketEvent.OrderId,
                            RecipientEmail = ticketEvent.CustomerEmail,
                            EventName = ticketEvent.EventName,
                            SeatNumber = ticketEvent.SeatNumber,
                            Price = ticketEvent.Price,
                            Currency = ticketEvent.Currency,
                            TicketPdfUrl = ticketEvent.TicketPdfUrl,
                            QrCodeData = ticketEvent.QrCodeData,
                            TicketIssuedAt = ticketEvent.IssuedAt
                        };

                        var result = await mediator.Send(command, stoppingToken);

                        if (result.Success)
                        {
                            _logger.LogInformation($"Notification sent for ticket {ticketEvent.TicketId}");
                        }
                        else
                        {
                            _logger.LogError($"Failed to send notification for ticket {ticketEvent.TicketId}: {result.Message}");
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Error processing ticket-issued event: {ex.Message}");
                }
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("TicketIssuedEventConsumer stopped");
        }
        finally
        {
            _consumer.Close();
        }
    }

    public override Task StopAsync(CancellationToken cancellationToken)
    {
        _consumer?.Dispose();
        return base.StopAsync(cancellationToken);
    }
}
