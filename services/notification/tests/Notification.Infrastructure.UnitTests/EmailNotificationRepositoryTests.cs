using Xunit;
using Microsoft.EntityFrameworkCore;
using Notification.Infrastructure.Persistence;
using Notification.Domain.Entities;
using FluentAssertions;
using System;
using System.Threading.Tasks;

namespace Notification.Infrastructure.UnitTests.Persistence;

public class EmailNotificationRepositoryTests
{
    private NotificationDbContext CreateContext()
    {
        // InMemoryDatabase doesnt support Relational methods like HasDefaultSchema
        // The error Migrations/EF reported earlier was because we were calling HasDefaultSchema on a non-relational builder
        var options = new DbContextOptionsBuilder<NotificationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new NotificationDbContext(options);
    }

    [Fact]
    public async Task AddAsync_ShouldAddNotification()
    {
        using var context = CreateContext();
        var repository = new EmailNotificationRepository(context);
        var notification = new EmailNotification 
        { 
            Id = Guid.NewGuid(), 
            OrderId = Guid.NewGuid(), 
            RecipientEmail = "test@example.com",
            Subject = "Test",
            Body = "Test",
            SentAt = DateTime.UtcNow
        };

        await repository.AddAsync(notification);
        await repository.SaveChangesAsync();

        var result = await context.EmailNotifications.FindAsync(notification.Id);
        result.Should().NotBeNull();
    }
}
