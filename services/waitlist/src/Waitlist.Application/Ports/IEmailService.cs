namespace Waitlist.Application.Ports;

public interface IEmailService
{
    Task SendWaitlistAssignmentAsync(string email, Guid seatId, DateTime expiresAt, CancellationToken cancellationToken = default);
}
