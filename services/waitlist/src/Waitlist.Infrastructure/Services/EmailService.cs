using Microsoft.Extensions.Logging;
using Waitlist.Application.Ports;

namespace Waitlist.Infrastructure.Services;

public class EmailService : IEmailService
{
    private readonly ILogger<EmailService> _logger;

    public EmailService(ILogger<EmailService> logger)
    {
        _logger = logger;
    }

    public Task SendWaitlistAssignmentAsync(
        string email, Guid seatId, DateTime expiresAt, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "[EMAIL] To: {Email} | Subject: Your seat is available! | SeatId: {SeatId} | ExpiresAt: {ExpiresAt:O}",
            email, seatId, expiresAt);

        return Task.CompletedTask;
    }
}
