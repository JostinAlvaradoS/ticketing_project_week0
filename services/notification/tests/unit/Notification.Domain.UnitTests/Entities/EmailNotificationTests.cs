using Notification.Domain.Entities;

namespace Notification.Domain.UnitTests.Entities;

public class EmailNotificationTests
{
    [Fact]
    public void CreateEmailNotification_WithValidData_ShouldSucceed()
    {
        // Arrange
        var id = Guid.NewGuid();
        var ticketId = Guid.NewGuid();
        var orderId = Guid.NewGuid();
        var recipientEmail = "customer@example.com";
        var subject = "Your Ticket";
        var body = "Ticket details...";
        var now = DateTime.UtcNow;

        // Act
        var notification = new EmailNotification
        {
            Id = id,
            TicketId = ticketId,
            OrderId = orderId,
            RecipientEmail = recipientEmail,
            Subject = subject,
            Body = body,
            Status = NotificationStatus.Pending,
            CreatedAt = now,
            UpdatedAt = now
        };

        // Assert
        notification.Id.Should().Be(id);
        notification.TicketId.Should().Be(ticketId);
        notification.OrderId.Should().Be(orderId);
        notification.RecipientEmail.Should().Be(recipientEmail);
        notification.Subject.Should().Be(subject);
        notification.Body.Should().Be(body);
        notification.Status.Should().Be(NotificationStatus.Pending);
        notification.SentAt.Should().BeNull();
    }

    [Fact]
    public void EmailNotification_WithSentStatus_ShouldHaveSentTimestamp()
    {
        // Arrange
        var notification = new EmailNotification
        {
            Id = Guid.NewGuid(),
            TicketId = Guid.NewGuid(),
            OrderId = Guid.NewGuid(),
            RecipientEmail = "customer@example.com",
            Subject = "Your Ticket",
            Body = "Details...",
            Status = NotificationStatus.Sent,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        var sentTime = DateTime.UtcNow;
        notification.SentAt = sentTime;

        // Act & Assert
        notification.Status.Should().Be(NotificationStatus.Sent);
        notification.SentAt.Should().Be(sentTime);
    }

    [Fact]
    public void EmailNotification_WithFailedStatus_ShouldHaveFailureReason()
    {
        // Arrange & Act
        var notification = new EmailNotification
        {
            Id = Guid.NewGuid(),
            TicketId = Guid.NewGuid(),
            OrderId = Guid.NewGuid(),
            RecipientEmail = "customer@example.com",
            Subject = "Your Ticket",
            Body = "Details...",
            Status = NotificationStatus.Failed,
            FailureReason = "SMTP timeout",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // Assert
        notification.Status.Should().Be(NotificationStatus.Failed);
        notification.FailureReason.Should().Be("SMTP timeout");
    }

    [Theory]
    [InlineData(NotificationStatus.Pending)]
    [InlineData(NotificationStatus.Sent)]
    [InlineData(NotificationStatus.Failed)]
    [InlineData(NotificationStatus.Retrying)]
    public void EmailNotification_ShouldSupportAllStatuses(NotificationStatus status)
    {
        // Arrange & Act
        var notification = new EmailNotification
        {
            Id = Guid.NewGuid(),
            TicketId = Guid.NewGuid(),
            OrderId = Guid.NewGuid(),
            RecipientEmail = "customer@example.com",
            Subject = "Your Ticket",
            Body = "Details...",
            Status = status,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // Assert
        notification.Status.Should().Be(status);
    }
}
