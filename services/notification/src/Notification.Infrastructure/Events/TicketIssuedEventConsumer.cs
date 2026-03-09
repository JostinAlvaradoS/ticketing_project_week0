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
    private readonly IConsumer<string, string>? _consumer;
    private readonly KafkaOptions _kafkaOptions;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<TicketIssuedEventConsumer> _logger;
    private readonly string _topicName;

    public TicketIssuedEventConsumer(
        IOptions<KafkaOptions> kafkaOptions,
        IServiceProvider serviceProvider,
        ILogger<TicketIssuedEventConsumer> logger,
        IConsumer<string, string>? consumer = null)
    {
        _kafkaOptions = kafkaOptions.Value;
        _serviceProvider = serviceProvider;
        _logger = logger;

        _topicName = _kafkaOptions.Topics.TryGetValue("TicketIssued", out var topic)
            ? topic
            : "ticket-issued";

        if (consumer != null)
        {
            _consumer = consumer;
        }
        else
        {
            try
            {
                var config = new ConsumerConfig
                {
                    BootstrapServers = _kafkaOptions.BootstrapServers,
                    GroupId = _kafkaOptions.ConsumerGroupId,
                    AutoOffsetReset = AutoOffsetReset.Earliest,
                    EnableAutoCommit = true,
                };

                _consumer = new ConsumerBuilder<string, string>(config).Build();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create Kafka consumer. Service will not be able to process events.");
            }
        }
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (_consumer == null)
        {
            _logger.LogWarning("Kafka consumer is null. ExecuteAsync will exit.");
            return;
        }

        _logger.LogInformation("TicketIssuedEventConsumer starting... (Topic: {Topic})", _topicName);
        
        try
        {
            _consumer.Subscribe(_topicName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to subscribe to Kafka topic {Topic}. Consumer will not run.", _topicName);
            return;
        }

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                var consumeResult = _consumer.Consume(TimeSpan.FromSeconds(1));

                if (consumeResult == null)
                {
                    await Task.Delay(100, stoppingToken);
                    continue;
                }

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
