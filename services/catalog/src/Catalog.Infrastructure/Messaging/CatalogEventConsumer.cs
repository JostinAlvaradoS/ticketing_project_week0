using System.Text.Json;
using Catalog.Application.Ports;
using Confluent.Kafka;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Catalog.Infrastructure.Messaging;

public class CatalogEventConsumer : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<CatalogEventConsumer> _logger;
    private readonly string _bootstrapServers;
    private readonly string[] _topics = { "reservation-created", "reservation-expired", "payment-succeeded" };
    private readonly string _groupId = "catalog-service-group";

    public CatalogEventConsumer(IServiceProvider serviceProvider, ILogger<CatalogEventConsumer> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _bootstrapServers = "speckit-kafka:9092";
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            // Give Kafka time to start up
            _logger.LogInformation("CatalogEventConsumer: Waiting for Kafka to be ready...");
            await Task.Delay(5000, stoppingToken);

            var config = new ConsumerConfig
            {
                BootstrapServers = _bootstrapServers,
                GroupId = _groupId,
                AutoOffsetReset = AutoOffsetReset.Earliest,
                EnableAutoCommit = true,
                SessionTimeoutMs = 30000
            };

            using var consumer = new ConsumerBuilder<string, string>(config).Build();
            consumer.Subscribe(_topics);

            _logger.LogInformation("CatalogEventConsumer started and subscribed to topics: {Topics}", string.Join(", ", _topics));

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var result = consumer.Consume(TimeSpan.FromSeconds(10));
                    if (result != null)
                    {
                        await ProcessEventAsync(result.Topic, result.Message.Value);
                    }
                }
                catch (OperationCanceledException)
                {
                    _logger.LogInformation("CatalogEventConsumer: Cancellation requested");
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error consuming Kafka message");
                    await Task.Delay(1000, stoppingToken);
                }
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("CatalogEventConsumer: Shutdown initiated");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "CatalogEventConsumer fatal error - service will continue without Kafka consumer");
        }
    }

    private async Task ProcessEventAsync(string topic, string message)
    {
        using var scope = _serviceProvider.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<ICatalogRepository>();

        try
        {
            var root = JsonDocument.Parse(message).RootElement;
            
            if (topic == "reservation-created")
            {
                if (root.TryGetProperty("seatId", out var seatIdProp) && Guid.TryParse(seatIdProp.GetString(), out var seatId))
                {
                    Guid? reservationId = null;
                    if (root.TryGetProperty("reservationId", out var resIdProp) && Guid.TryParse(resIdProp.GetString(), out var resId))
                    {
                        reservationId = resId;
                    }
                    await repository.UpdateSeatStatusAsync(seatId, "reserved", reservationId);
                }
            }
            else if (topic == "reservation-expired")
            {
                if (root.TryGetProperty("seatId", out var seatIdProp) && Guid.TryParse(seatIdProp.GetString(), out var seatId))
                {
                    await repository.UpdateSeatStatusAsync(seatId, "available");
                }
                else if (root.TryGetProperty("reservationId", out var resIdProp) && Guid.TryParse(resIdProp.GetString(), out var resId))
                {
                    await repository.UpdateSeatStatusByReservationAsync(resId, "available");
                }
            }
            else if (topic == "payment-succeeded")
            {
                if (root.TryGetProperty("reservationId", out var resIdProp) && Guid.TryParse(resIdProp.GetString(), out var resId))
                {
                    await repository.UpdateSeatStatusByReservationAsync(resId, "sold");
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing event {Topic}", topic);
        }
    }
}

public interface ICatalogRepositoryExtras
{
    // Adding this placeholder to denote we need to update the interface
}
