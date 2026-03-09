using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Notification.Application.Ports;

namespace Notification.Infrastructure.Email;

public class SmtpEmailOptions
{
    public const string Section = "Email:Smtp";
    public string Host { get; set; } = "localhost";
    public int Port { get; set; } = 587;
    public string Username { get; set; } = "";
    public string Password { get; set; } = "";
    public string FromAddress { get; set; } = "noreply@ticketing.local";
    public string FromName { get; set; } = "Ticketing Platform";
    public bool EnableSsl { get; set; } = true;
    public bool UseDevMode { get; set; } = true;
}

public class SmtpEmailService : IEmailService
{
    private readonly SmtpEmailOptions _options;
    private readonly ILogger<SmtpEmailService> _logger;

    public SmtpEmailService(IOptions<SmtpEmailOptions> options, ILogger<SmtpEmailService> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public async Task<bool> SendAsync(string recipientEmail, string subject, string body, string? attachmentPath = null)
    {
        try
        {
            if (_options.UseDevMode)
            {
                // In dev mode, just log the email instead of sending
                _logger.LogInformation($"[DEV MODE] Email queued for sending");
                _logger.LogInformation($"  To: {recipientEmail}");
                _logger.LogInformation($"  Subject: {subject}");
                _logger.LogInformation($"  Attachment: {attachmentPath ?? "none"}");
                return true;
            }

            // TODO: Implement actual SMTP sending using System.Net.Mail.SmtpClient
            // For now, just log and return true to simulate success
            _logger.LogInformation($"[PRODUCTION MODE] Email would be sent to {recipientEmail}");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error sending email to {recipientEmail}: {ex.Message}");
            return false;
        }
    }
}
