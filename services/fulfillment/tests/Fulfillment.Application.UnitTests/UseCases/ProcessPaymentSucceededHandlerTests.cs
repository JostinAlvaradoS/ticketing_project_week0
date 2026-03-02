using Fulfillment.Application.Ports;
using Fulfillment.Application.UseCases.ProcessPaymentSucceeded;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using FluentAssertions;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Fulfillment.Application.UnitTests.UseCases;

public class ProcessPaymentSucceededHandlerTests
{
    private readonly Mock<ILogger<ProcessPaymentSucceededHandler>> _loggerMock;
    private readonly Mock<IOrderingServiceClient> _orderingServiceMock;
    private readonly Mock<ITicketRepository> _ticketRepositoryMock;
    private readonly ProcessPaymentSucceededHandler _handler;

    public ProcessPaymentSucceededHandlerTests()
    {
        _loggerMock = new Mock<ILogger<ProcessPaymentSucceededHandler>>();
        _orderingServiceMock = new Mock<IOrderingServiceClient>();
        _ticketRepositoryMock = new Mock<ITicketRepository>();
        _handler = new ProcessPaymentSucceededHandler(
            _loggerMock.Object,
            _orderingServiceMock.Object,
            _ticketRepositoryMock.Object);
    }

    [Fact]
    public async Task Handle_ShouldReturnSuccess_WhenCommandIsValid()
    {
        // Arrange
        var command = new ProcessPaymentSucceededCommand
        {
            OrderId = Guid.NewGuid(),
            CustomerEmail = "test@example.com",
            EventId = Guid.NewGuid(),
            EventName = "Test Event",
            SeatNumber = "A1",
            Price = 100.00m,
            Currency = "USD"
        };

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.Message.Should().Be("Ticket generated successfully");
        
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains($"Processing payment for order {command.OrderId}")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }
}
