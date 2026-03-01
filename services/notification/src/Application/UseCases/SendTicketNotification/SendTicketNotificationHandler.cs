using MediatR;
using Microsoft.Extensions.Logging;
using Notification.Application.Ports;
using Notification.Domain.Entities;

namespace Notification.Application.UseCases.SendTicketNotification;

public class SendTicketNotificationCommand : IRequest<SendTicketNotificationResponse>
{
    public Guid TicketId { get; set; }
    public Guid OrderId { get; set; }
    public string RecipientEmail { get; set; } = string.Empty;
    public string EventName { get; set; } = string.Empty;
    public string SeatNumber { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string Currency { get; set; } = "USD";
    public string? TicketPdfUrl { get; set; }
    public string? QrCodeData { get; set; }
    public DateTime TicketIssuedAt { get; set; }
}

public class SendTicketNotificationResponse
{
    public Guid NotificationId { get; set; }
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
}

public class SendTicketNotificationHandler : IRequestHandler<SendTicketNotificationCommand, SendTicketNotificationResponse>
{
    private readonly IEmailNotificationRepository _repository;
    private readonly IEmailService _emailService;
    private readonly ILogger<SendTicketNotificationHandler> _logger;

    public SendTicketNotificationHandler(
        IEmailNotificationRepository repository,
        IEmailService emailService,
        ILogger<SendTicketNotificationHandler> logger)
    {
        _repository = repository;
        _emailService = emailService;
        _logger = logger;
    }

    public async Task<SendTicketNotificationResponse> Handle(SendTicketNotificationCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation($"Processing ticket notification for order {request.OrderId} to {request.RecipientEmail}");

        try
        {
            // Check if notification already exists (idempotency)
            var existingNotification = await _repository.GetByOrderIdAsync(request.OrderId);
            if (existingNotification != null)
            {
                _logger.LogInformation($"Notification already exists for order {request.OrderId}");
                return new SendTicketNotificationResponse
                {
                    NotificationId = existingNotification.Id,
                    Success = true,
                    Message = "Notification already sent"
                };
            }

            // Build email content
            var subject = $"Your Ticket for {request.EventName}";
            var body = BuildEmailBody(request);

            // Send email
            var emailSent = await _emailService.SendAsync(
                request.RecipientEmail,
                subject,
                body,
                request.TicketPdfUrl);

            // Create and persist notification record
            var notification = new EmailNotification
            {
                Id = Guid.NewGuid(),
                TicketId = request.TicketId,
                OrderId = request.OrderId,
                RecipientEmail = request.RecipientEmail,
                Subject = subject,
                Body = body,
                TicketPdfUrl = request.TicketPdfUrl,
                Status = emailSent ? NotificationStatus.Sent : NotificationStatus.Failed,
                CreatedAt = DateTime.UtcNow,
                SentAt = emailSent ? DateTime.UtcNow : null,
                UpdatedAt = DateTime.UtcNow,
                FailureReason = emailSent ? null : "Email service failed"
            };

            await _repository.AddAsync(notification);
            await _repository.SaveChangesAsync();

            if (emailSent)
            {
                _logger.LogInformation($"Notification sent and persisted for order {request.OrderId}");
            }
            else
            {
                _logger.LogWarning($"Notification queued but email send failed for order {request.OrderId}");
            }

            return new SendTicketNotificationResponse
            {
                NotificationId = notification.Id,
                Success = true,
                Message = emailSent ? "Notification sent successfully" : "Notification queued (email send failed)"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error processing notification for order {request.OrderId}: {ex.Message}");
            return new SendTicketNotificationResponse
            {
                Success = false,
                Message = $"Error: {ex.Message}"
            };
        }
    }

    private string BuildEmailBody(SendTicketNotificationCommand request)
    {
        return $@"
Dear Customer,

Thank you for your purchase! Your ticket has been successfully issued.

Event Details:
- Event: {request.EventName}
- Seat: {request.SeatNumber}
- Price: {request.Price} {request.Currency}
- Issued At: {request.TicketIssuedAt:g}

Your ticket PDF is attached to this email. Please download it and bring it to the venue.
You can also scan the QR code on your ticket for quick entry.

If you have any questions, please contact our support team.

Best regards,
Ticketing Platform
";
    }
}
