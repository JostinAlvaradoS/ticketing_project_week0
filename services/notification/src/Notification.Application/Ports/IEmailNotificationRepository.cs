using Notification.Domain.Entities;

namespace Notification.Application.Ports;

public interface IEmailNotificationRepository
{
    Task<EmailNotification?> GetByIdAsync(Guid id);
    Task<EmailNotification?> GetByOrderIdAsync(Guid orderId);
    Task<EmailNotification> AddAsync(EmailNotification notification);
    Task<EmailNotification> UpdateAsync(EmailNotification notification);
    Task<bool> SaveChangesAsync();
}
