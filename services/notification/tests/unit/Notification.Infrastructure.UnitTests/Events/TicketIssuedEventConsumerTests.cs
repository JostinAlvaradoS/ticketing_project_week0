using Confluent.Kafka;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Notification.Application.UseCases.SendTicketNotification;
using Notification.Infrastructure.Events;
using System.Text.Json;
using Xunit;
using FluentAssertions;

namespace Notification.Infrastructure.UnitTests.Events;

public class TicketIssuedEventConsumerTests
{
    private readonly Mock<IConsumer<string, string>> _mockConsumer;
    private readonly Mock<IMediator> _mockMediator;
    private readonly Mock<ILogger<TicketIssuedEventConsumer>> _mockLogger;
    private readonly Mock<IServiceProvider> _mockServiceProvider;
    private readonly Mock<IServiceScope> _mockServiceScope;
    private readonly Mock<IServiceScopeFactory> _mockServiceScopeFactory;
    private readonly KafkaOptions _kafkaOptions;

    public TicketIssuedEventConsumerTests()
    {
        _mockConsumer = new Mock<IConsumer<string, string>>();
        _mockMediator = new Mock<IMediator>();
        _mockLogger = new Mock<ILogger<TicketIssuedEventConsumer>>();
        _mockServiceProvider = new Mock<IServiceProvider>();
        _mockServiceScope = new Mock<IServiceScope>();
        _mockServiceScopeFactory = new Mock<IServiceScopeFactory>();

        _kafkaOptions = new KafkaOptions
        {
            BootstrapServers = "localhost:9092",
            ConsumerGroupId = "test-group",
            Topics = new Dictionary<string, string> { { "TicketIssued", "ticket-issued" } }
        };

        // Setup ServiceProvider for Scoping
        _mockServiceProvider.Setup(x => x.GetService(typeof(IServiceScopeFactory)))
            .Returns(_mockServiceScopeFactory.Object);
        _mockServiceScopeFactory.Setup(x => x.CreateScope())
            .Returns(_mockServiceScope.Object);
        _mockServiceScope.Setup(x => x.ServiceProvider)
            .Returns(_mockServiceProvider.Object);
        _mockServiceProvider.Setup(x => x.GetService(typeof(IMediator)))
            .Returns(_mockMediator.Object);
    }

    /// <summary>
    /// This test uses a reflected wrapper or a modified consumer approach 
    /// because the original class builds the consumer in the constructor.
    /// Since we can't easily inject a mock into the private _consumer field,
    /// we test the event processing logic found in the ExecuteAsync loop.
    /// </summary>
    [Fact]
    public async Task ExecuteAsync_WhenEventReceived_ShouldSendMediatorCommand()
    {
        // Arrange
        var ticketEvent = new TicketIssuedEvent
        {
            TicketId = Guid.NewGuid(),
            OrderId = Guid.NewGuid(),
            CustomerEmail = "test@example.com",
            EventName = "Rock Concert",
            SeatNumber = "A1",
            Price = 100.00m,
            Currency = "USD",
            IssuedAt = DateTime.UtcNow
        };

        var json = JsonSerializer.Serialize(ticketEvent, new JsonSerializerOptions 
        { 
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase 
        });

        var consumeResult = new ConsumeResult<string, string>
        {
            Message = new Message<string, string> { Key = "test-key", Value = json },
            Topic = "ticket-issued"
        };

        _mockConsumer.Setup(x => x.Consume(It.IsAny<TimeSpan>()))
            .Returns(consumeResult);

        _mockMediator.Setup(x => x.Send(It.IsAny<SendTicketNotificationCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SendTicketNotificationResponse { Success = true });

        // Since background service starts a loop, we create a specialized consumer
        // that we can inject the mock into for testing purposes.
        var optionsMock = new Mock<IOptions<KafkaOptions>>();
        optionsMock.Setup(x => x.Value).Returns(_kafkaOptions);

        var service = new TestableTicketIssuedEventConsumer(
            optionsMock.Object,
            _mockServiceProvider.Object,
            _mockLogger.Object,
            _mockConsumer.Object);

        var cts = new CancellationTokenSource();
        
        // Act
        // Run the service briefly and then cancel
        var task = service.TestExecuteAsync(cts.Token);
        await Task.Delay(200); // Give it time to process one message
        cts.Cancel();

        // Assert
        _mockMediator.Verify(x => x.Send(
            It.Is<SendTicketNotificationCommand>(c => 
                c.TicketId == ticketEvent.TicketId && 
                c.RecipientEmail == ticketEvent.CustomerEmail),
            It.IsAny<CancellationToken>()), 
            Times.AtLeastOnce);
            
        _mockLogger.Verify(x => x.Log(
            LogLevel.Information,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Notification sent")),
            It.IsAny<Exception?>(),
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce);
    }

    /// <summary>
    /// Testable version of the consumer that allows injecting the mock consumer.
    /// In a real production scenario, the consumer would be injected via a factory.
    /// </summary>
    private class TestableTicketIssuedEventConsumer : TicketIssuedEventConsumer
    {
        private readonly IConsumer<string, string> _injectedConsumer;

        public TestableTicketIssuedEventConsumer(
            IOptions<KafkaOptions> kafkaOptions,
            IServiceProvider serviceProvider,
            ILogger<TicketIssuedEventConsumer> logger,
            IConsumer<string, string> consumer) 
            : base(kafkaOptions, serviceProvider, logger)
        {
            _injectedConsumer = consumer;
            
            // Use reflection to overwrite the private _consumer constructed by the base class
            var field = typeof(TicketIssuedEventConsumer).GetField("_consumer", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            field?.SetValue(this, _injectedConsumer);
        }

        public Task TestExecuteAsync(CancellationToken stoppingToken)
        {
            return ExecuteAsync(stoppingToken);
        }
    }
}
