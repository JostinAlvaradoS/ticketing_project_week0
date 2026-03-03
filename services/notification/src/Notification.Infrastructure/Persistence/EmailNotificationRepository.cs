using Microsoft.EntityFrameworkCore;
using Notification.Application.Ports;
using Notification.Domain.Entities;

namespace Notification.Infrastructure.Persistence;

public class EmailNotificationRepository : IEmailNotificationRepository
{
    private readonly NotificationDbContext _context;

    public EmailNotificationRepository(NotificationDbContext context)
    {
        _context = context;
    }

    public async Task<EmailNotification?> GetByIdAsync(Guid id)
    {
        return await _context.EmailNotifications.FindAsync(id);
    }

    public async Task<EmailNotification?> GetByOrderIdAsync(Guid orderId)
    {
        return await _context.EmailNotifications
            .FirstOrDefaultAsync(n => n.OrderId == orderId);
    }

    public async Task<EmailNotification> AddAsync(EmailNotification notification)
    {
        await _context.EmailNotifications.AddAsync(notification);
        return notification;
    }

    public async Task<EmailNotification> UpdateAsync(EmailNotification notification)
    {
        _context.EmailNotifications.Update(notification);
        return await Task.FromResult(notification);
    }

    public async Task<bool> SaveChangesAsync()
    {
        var result = await _context.SaveChangesAsync();
        return result > 0;
    }
}
