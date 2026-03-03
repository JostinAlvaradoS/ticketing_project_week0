using Microsoft.EntityFrameworkCore;
using Notification.Domain.Entities;
using Notification.Infrastructure.Persistence;

namespace Notification.IntegrationTests.Persistence;

public class EmailNotificationRepositoryTests
{
    private NotificationDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<NotificationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new NotificationDbContext(options);
    }

    [Fact]
    public async Task AddAsync_WithValidNotification_ShouldSucceed()
    {
        // Arrange
        using var context = CreateDbContext();
        var repository = new EmailNotificationRepository(context);
        var orderId = Guid.NewGuid();
        var notification = new EmailNotification
        {
            Id = Guid.NewGuid(),
            TicketId = Guid.NewGuid(),
            OrderId = orderId,
            RecipientEmail = "customer@example.com",
            Subject = "Your Ticket",
            Body = "Ticket details...",
            Status = NotificationStatus.Pending,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // Act
        var result = await repository.AddAsync(notification);
        await repository.SaveChangesAsync();

        // Assert
        result.Id.Should().Be(notification.Id);
        var retrieved = await repository.GetByIdAsync(notification.Id);
        retrieved.Should().NotBeNull();
        retrieved!.RecipientEmail.Should().Be("customer@example.com");
    }

    [Fact]
    public async Task GetByOrderIdAsync_WithExistingOrder_ShouldReturnNotification()
    {
        // Arrange
        using var context = CreateDbContext();
        var repository = new EmailNotificationRepository(context);
        var orderId = Guid.NewGuid();
        var notification = new EmailNotification
        {
            Id = Guid.NewGuid(),
            TicketId = Guid.NewGuid(),
            OrderId = orderId,
            RecipientEmail = "customer@example.com",
            Subject = "Ticket",
            Body = "Details",
            Status = NotificationStatus.Sent,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await repository.AddAsync(notification);
        await repository.SaveChangesAsync();

        // Act
        var result = await repository.GetByOrderIdAsync(orderId);

        // Assert
        result.Should().NotBeNull();
        result!.OrderId.Should().Be(orderId);
    }

    [Fact]
    public async Task GetByOrderIdAsync_WithNonExistentOrder_ShouldReturnNull()
    {
        // Arrange
        using var context = CreateDbContext();
        var repository = new EmailNotificationRepository(context);
        var nonExistentOrderId = Guid.NewGuid();

        // Act
        var result = await repository.GetByOrderIdAsync(nonExistentOrderId);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task UpdateAsync_WithExistingNotification_ShouldUpdateSuccessfully()
    {
        // Arrange
        using var context = CreateDbContext();
        var repository = new EmailNotificationRepository(context);
        var orderId = Guid.NewGuid();
        var notification = new EmailNotification
        {
            Id = Guid.NewGuid(),
            TicketId = Guid.NewGuid(),
            OrderId = orderId,
            RecipientEmail = "customer@example.com",
            Subject = "Ticket",
            Body = "Details",
            Status = NotificationStatus.Pending,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await repository.AddAsync(notification);
        await repository.SaveChangesAsync();

        // Act
        notification.Status = NotificationStatus.Sent;
        notification.SentAt = DateTime.UtcNow;
        notification.UpdatedAt = DateTime.UtcNow;
        await repository.UpdateAsync(notification);
        await repository.SaveChangesAsync();

        // Assert
        var updated = await repository.GetByIdAsync(notification.Id);
        updated.Should().NotBeNull();
        updated!.Status.Should().Be(NotificationStatus.Sent);
        updated.SentAt.Should().NotBeNull();
    }

    [Fact]
    public async Task SaveChangesAsync_ShouldReturnTrueWhenChangesExist()
    {
        // Arrange
        using var context = CreateDbContext();
        var repository = new EmailNotificationRepository(context);
        var notification = new EmailNotification
        {
            Id = Guid.NewGuid(),
            TicketId = Guid.NewGuid(),
            OrderId = Guid.NewGuid(),
            RecipientEmail = "customer@example.com",
            Subject = "Ticket",
            Body = "Details",
            Status = NotificationStatus.Sent,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await repository.AddAsync(notification);

        // Act
        var result = await repository.SaveChangesAsync();

        // Assert
        result.Should().BeTrue();
    }
}

// Helper to check if InMemoryDatabase supports the saved changes
internal static class RepositoryTestHelper
{
    public static bool ValidateEmailNotification(EmailNotification notification)
    {
        return !string.IsNullOrEmpty(notification.RecipientEmail) &&
               !string.IsNullOrEmpty(notification.Subject) &&
               !string.IsNullOrEmpty(notification.Body);
    }
}
