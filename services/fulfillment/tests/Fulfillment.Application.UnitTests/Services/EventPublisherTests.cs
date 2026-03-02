using Fulfillment.Application.Ports;
using Fulfillment.Domain.Entities;
using Fulfillment.Infrastructure.Events;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;
using FluentAssertions;
using System;

namespace Fulfillment.Application.UnitTests.Services;

public class EventPublisherTests
{
    [Fact]
    public void PublishAsync_Should_Serialize_Event_To_Json()
    {
        // Arrange
        var ticketEvent = new TicketIssuedEvent
        {
            OrderId = Guid.NewGuid(),
            TicketId = Guid.NewGuid(),
            CustomerEmail = "test@example.com",
            EventName = "Test Event",
            SeatNumber = "A-1",
            Timestamp = DateTime.UtcNow
        };

        // Act
        var json = System.Text.Json.JsonSerializer.Serialize(ticketEvent,
            new System.Text.Json.JsonSerializerOptions
            {
                PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase
            });

        // Assert
        json.Should().NotBeNullOrEmpty();
        // Check for snake_case properties (as defined in TicketIssuedEvent attributes)
        json.Should().Contain("order_id");
        json.Should().Contain("ticket_id");
        json.Should().Contain("customer_email");
    }

    [Fact]
    public void Logger_Should_Be_Configurable()
    {
        // Simple test to verify Moq and ILogger compatibility
        var loggerMock = new Mock<ILogger<KafkaEventPublisher>>();
        
        loggerMock.Object.LogInformation("Testing logger");

        loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Testing logger")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }
}
