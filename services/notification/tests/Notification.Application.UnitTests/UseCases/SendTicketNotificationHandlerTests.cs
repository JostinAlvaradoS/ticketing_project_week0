using Microsoft.Extensions.Logging;
using Moq;
using Notification.Application.Ports;
using Notification.Application.UseCases.SendTicketNotification;
using Notification.Domain.Entities;

namespace Notification.Application.UnitTests.UseCases;

public class SendTicketNotificationHandlerTests
{
    private readonly Mock<IEmailNotificationRepository> _repositoryMock;
    private readonly Mock<IEmailService> _emailServiceMock;
    private readonly Mock<ILogger<SendTicketNotificationHandler>> _loggerMock;
    private readonly SendTicketNotificationHandler _handler;

    public SendTicketNotificationHandlerTests()
    {
        _repositoryMock = new Mock<IEmailNotificationRepository>();
        _emailServiceMock = new Mock<IEmailService>();
        _loggerMock = new Mock<ILogger<SendTicketNotificationHandler>>();
        _handler = new SendTicketNotificationHandler(_repositoryMock.Object, _emailServiceMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task Handle_WithValidCommand_ShouldSendEmailAndPersistNotification()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var ticketId = Guid.NewGuid();
        var customerEmail = "customer@example.com";
        var command = new SendTicketNotificationCommand
        {
            TicketId = ticketId,
            OrderId = orderId,
            RecipientEmail = customerEmail,
            EventName = "Concert 2026",
            SeatNumber = "A1",
            Price = 100.00m,
            Currency = "USD",
            TicketPdfUrl = "https://example.com/ticket.pdf",
            QrCodeData = "QR_DATA_HERE",
            TicketIssuedAt = DateTime.UtcNow
        };

        _repositoryMock
            .Setup(r => r.GetByOrderIdAsync(orderId))
            .ReturnsAsync((EmailNotification?)null);

        _emailServiceMock
            .Setup(e => e.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(true);

        _repositoryMock
            .Setup(r => r.AddAsync(It.IsAny<EmailNotification>()))
            .ReturnsAsync((EmailNotification notification) => notification);

        _repositoryMock
            .Setup(r => r.SaveChangesAsync())
            .ReturnsAsync(true);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Success.Should().BeTrue();
        result.Message.Should().Contain("successfully");
        result.NotificationId.Should().NotBeEmpty();

        _emailServiceMock.Verify(
            e => e.SendAsync(customerEmail, It.IsAny<string>(), It.IsAny<string>(), command.TicketPdfUrl),
            Times.Once);

        _repositoryMock.Verify(r => r.AddAsync(It.IsAny<EmailNotification>()), Times.Once);
        _repositoryMock.Verify(r => r.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task Handle_WithExistingNotification_ShouldReturnIdempotentResult()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var existingNotificationId = Guid.NewGuid();
        var command = new SendTicketNotificationCommand
        {
            TicketId = Guid.NewGuid(),
            OrderId = orderId,
            RecipientEmail = "customer@example.com",
            EventName = "Concert 2026",
            SeatNumber = "A1",
            Price = 100.00m,
            Currency = "USD",
            TicketIssuedAt = DateTime.UtcNow
        };

        var existingNotification = new EmailNotification
        {
            Id = existingNotificationId,
            OrderId = orderId,
            TicketId = command.TicketId,
            RecipientEmail = command.RecipientEmail,
            Status = NotificationStatus.Sent
        };

        _repositoryMock
            .Setup(r => r.GetByOrderIdAsync(orderId))
            .ReturnsAsync(existingNotification);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Success.Should().BeTrue();
        result.NotificationId.Should().Be(existingNotificationId);
        result.Message.Should().Contain("already");

        _emailServiceMock.Verify(e => e.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        _repositoryMock.Verify(r => r.AddAsync(It.IsAny<EmailNotification>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenEmailServiceFails_ShouldStillPersistNotificationWithFailedStatus()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var ticketId = Guid.NewGuid();
        var customerEmail = "customer@example.com";
        var command = new SendTicketNotificationCommand
        {
            TicketId = ticketId,
            OrderId = orderId,
            RecipientEmail = customerEmail,
            EventName = "Concert 2026",
            SeatNumber = "A1",
            Price = 100.00m,
            Currency = "USD",
            TicketIssuedAt = DateTime.UtcNow
        };

        _repositoryMock
            .Setup(r => r.GetByOrderIdAsync(orderId))
            .ReturnsAsync((EmailNotification?)null);

        _emailServiceMock
            .Setup(e => e.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(false);

        EmailNotification? capturedNotification = null;
        _repositoryMock
            .Setup(r => r.AddAsync(It.IsAny<EmailNotification>()))
            .Callback<EmailNotification>(n => capturedNotification = n)
            .ReturnsAsync((EmailNotification notification) => notification);

        _repositoryMock
            .Setup(r => r.SaveChangesAsync())
            .ReturnsAsync(true);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Success.Should().BeTrue();
        result.Message.Should().Contain("queued");

        capturedNotification.Should().NotBeNull();
        capturedNotification!.Status.Should().Be(NotificationStatus.Failed);
        capturedNotification.FailureReason.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task Handle_WithException_ShouldReturnFailureResult()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var command = new SendTicketNotificationCommand
        {
            TicketId = Guid.NewGuid(),
            OrderId = orderId,
            RecipientEmail = "customer@example.com",
            EventName = "Concert 2026",
            SeatNumber = "A1",
            Price = 100.00m,
            Currency = "USD",
            TicketIssuedAt = DateTime.UtcNow
        };

        _repositoryMock
            .Setup(r => r.GetByOrderIdAsync(orderId))
            .ThrowsAsync(new InvalidOperationException("Database error"));

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("Error");
    }
}
