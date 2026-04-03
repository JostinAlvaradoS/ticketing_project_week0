namespace Waitlist.Application.Ports;

public interface IEmailService
{
    Task<bool> SendAsync(string recipientEmail, string subject, string body, string? attachmentPath = null);
}
