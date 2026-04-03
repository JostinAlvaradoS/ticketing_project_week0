using Microsoft.Extensions.Logging;
using Waitlist.Application.Ports;

namespace Waitlist.Infrastructure.Clients;

// Stub implementation — in production replace with real SMTP/SendGrid adapter
public class SmtpEmailService : IEmailService
{
    private readonly ILogger<SmtpEmailService> _logger;

    public SmtpEmailService(ILogger<SmtpEmailService> logger) => _logger = logger;

    public Task<bool> SendAsync(string recipientEmail, string subject, string body, string? attachmentPath = null)
    {
        _logger.LogInformation("EMAIL to {Recipient} | Subject: {Subject}", recipientEmail, subject);
        return Task.FromResult(true);
    }
}
