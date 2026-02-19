using Microsoft.Extensions.Logging;
using PaymentService.Application.Dtos;
using PaymentService.Application.Ports.Inbound;
using PaymentService.Application.Ports.Outbound;
using PaymentService.Domain.Entities;
using PaymentService.Domain.Enums;

namespace PaymentService.Application.UseCases;

public class ProcessPaymentRejectedUseCase : IProcessPaymentRejectedUseCase
{
    private readonly ITicketRepository _ticketRepository;
    private readonly IPaymentRepository _paymentRepository;
    private readonly ITicketHistoryRepository _historyRepository;
    private readonly ILogger<ProcessPaymentRejectedUseCase> _logger;

    public ProcessPaymentRejectedUseCase(
        ITicketRepository ticketRepository,
        IPaymentRepository paymentRepository,
        ITicketHistoryRepository historyRepository,
        ILogger<ProcessPaymentRejectedUseCase> logger)
    {
        _ticketRepository = ticketRepository;
        _paymentRepository = paymentRepository;
        _historyRepository = historyRepository;
        _logger = logger;
    }

    public async Task<ValidationResult> ExecuteAsync(PaymentRejectedEventDto paymentEvent)
    {
        var ticket = await _ticketRepository.GetByIdAsync(paymentEvent.TicketId);

        if (ticket == null)
        {
            _logger.LogWarning("Ticket {TicketId} not found", paymentEvent.TicketId);
            return ValidationResult.Failure("Ticket not found");
        }

        if (ticket.Status == TicketStatus.released)
        {
            _logger.LogInformation("Ticket {TicketId} already released. Skipping duplicate event", paymentEvent.TicketId);
            return ValidationResult.AlreadyProcessed();
        }

        var success = await TransitionToReleasedAsync(paymentEvent.TicketId, $"Payment rejected: {paymentEvent.RejectionReason}");

        if (success)
        {
            _logger.LogInformation("Payment rejection processed for ticket {TicketId}", paymentEvent.TicketId);
            return ValidationResult.Success();
        }

        return ValidationResult.Failure("Failed to transition ticket to released status");
    }

    private async Task<bool> TransitionToReleasedAsync(long ticketId, string reason)
    {
        var ticket = await _ticketRepository.GetByIdForUpdateAsync(ticketId);

        if (ticket == null)
        {
            _logger.LogWarning("Ticket {TicketId} not found", ticketId);
            return false;
        }

        var oldStatus = ticket.Status;
        ticket.Status = TicketStatus.released;

        var updated = await _ticketRepository.UpdateAsync(ticket);

        if (!updated)
        {
            var current = await _ticketRepository.GetByIdAsync(ticketId);
            if (current != null && current.Status == TicketStatus.released)
            {
                _logger.LogInformation("Ticket {TicketId} already released (idempotent event).", ticketId);
                return true;
            }
            _logger.LogWarning("Failed to update ticket {TicketId} - real concurrent modification", ticketId);
            return false;
        }

        var payment = await _paymentRepository.GetByTicketIdAsync(ticketId);
        if (payment != null && payment.Status == PaymentStatus.pending)
        {
            payment.Status = PaymentStatus.failed;
            payment.UpdatedAt = DateTime.UtcNow;
            await _paymentRepository.UpdateAsync(payment);
        }

        await RecordHistoryAsync(ticketId, oldStatus, TicketStatus.released, reason);
        _logger.LogInformation("Ticket {TicketId} successfully transitioned to Released. Reason: {Reason}", ticketId, reason);
        return true;
    }

    private async Task RecordHistoryAsync(long ticketId, TicketStatus oldStatus, TicketStatus newStatus, string reason)
    {
        var history = new TicketHistory
        {
            TicketId = ticketId,
            OldStatus = oldStatus,
            NewStatus = newStatus,
            ChangedAt = DateTime.UtcNow,
            Reason = reason
        };

        await _historyRepository.AddAsync(history);
    }
}
