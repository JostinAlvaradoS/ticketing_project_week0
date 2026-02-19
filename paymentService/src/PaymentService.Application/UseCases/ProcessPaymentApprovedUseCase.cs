using Microsoft.Extensions.Logging;
using PaymentService.Application.Dtos;
using PaymentService.Application.Ports.Inbound;
using PaymentService.Application.Ports.Outbound;
using PaymentService.Domain.Entities;
using PaymentService.Domain.Enums;

namespace PaymentService.Application.UseCases;

public class ProcessPaymentApprovedUseCase : IProcessPaymentApprovedUseCase
{
    private readonly ITicketRepository _ticketRepository;
    private readonly IPaymentRepository _paymentRepository;
    private readonly ITicketHistoryRepository _historyRepository;
    private readonly ILogger<ProcessPaymentApprovedUseCase> _logger;

    public ProcessPaymentApprovedUseCase(
        ITicketRepository ticketRepository,
        IPaymentRepository paymentRepository,
        ITicketHistoryRepository historyRepository,
        ILogger<ProcessPaymentApprovedUseCase> logger)
    {
        _ticketRepository = ticketRepository;
        _paymentRepository = paymentRepository;
        _historyRepository = historyRepository;
        _logger = logger;
    }

    public async Task<ValidationResult> ExecuteAsync(PaymentApprovedEventDto paymentEvent)
    {
        var ticket = await _ticketRepository.GetByIdAsync(paymentEvent.TicketId);

        if (ticket == null)
        {
            _logger.LogWarning("Ticket {TicketId} not found", paymentEvent.TicketId);
            return ValidationResult.Failure("Ticket not found");
        }

        if (ticket.Status == TicketStatus.paid)
        {
            _logger.LogInformation("Ticket {TicketId} already paid. Skipping duplicate event", paymentEvent.TicketId);
            return ValidationResult.AlreadyProcessed();
        }

        if (ticket.Status != TicketStatus.reserved)
        {
            _logger.LogWarning("Invalid ticket status for payment. TicketId: {TicketId}, Status: {Status}", paymentEvent.TicketId, ticket.Status);
            return ValidationResult.Failure($"Invalid ticket status: {ticket.Status}");
        }

        if (ticket.ReservedAt == null || !IsWithinTimeLimit(ticket.ReservedAt.Value, paymentEvent.ApprovedAt))
        {
            _logger.LogWarning("Payment received after TTL. TicketId: {TicketId}", paymentEvent.TicketId);
            await TransitionToReleasedAsync(paymentEvent.TicketId, "Payment received after TTL");
            return ValidationResult.Failure("TTL exceeded");
        }

        var payment = await _paymentRepository.GetByTicketIdAsync(paymentEvent.TicketId);
        if (payment == null)
        {
            payment = await _paymentRepository.CreateAsync(new Payment
            {
                TicketId = paymentEvent.TicketId,
                Status = PaymentStatus.pending,
                AmountCents = paymentEvent.AmountCents,
                Currency = paymentEvent.Currency,
                ProviderRef = paymentEvent.TransactionRef,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });
        }

        var success = await TransitionToPaidAsync(paymentEvent.TicketId, paymentEvent.TransactionRef);

        if (success)
        {
            _logger.LogInformation("Payment processed successfully for ticket {TicketId}", paymentEvent.TicketId);
            return ValidationResult.Success();
        }

        return ValidationResult.Failure("Failed to transition ticket to paid status");
    }

    private async Task<bool> TransitionToPaidAsync(long ticketId, string providerRef)
    {
        var ticket = await _ticketRepository.GetByIdForUpdateAsync(ticketId);

        if (ticket == null || ticket.Status != TicketStatus.reserved)
        {
            _logger.LogWarning("Cannot transition to paid - invalid state. TicketId: {TicketId}, Status: {Status}", ticketId, ticket?.Status);
            return false;
        }

        var oldStatus = ticket.Status;
        ticket.Status = TicketStatus.paid;
        ticket.PaidAt = DateTime.UtcNow;

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
        if (payment != null)
        {
            payment.Status = PaymentStatus.approved;
            payment.ProviderRef = providerRef;
            payment.UpdatedAt = DateTime.UtcNow;
            await _paymentRepository.UpdateAsync(payment);
        }

        await RecordHistoryAsync(ticketId, oldStatus, TicketStatus.paid, "Payment approved");

        _logger.LogInformation("Ticket {TicketId} successfully transitioned to Paid with provider ref {ProviderRef}", ticketId, providerRef);
        return true;
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
            return false;
        }

        var payment = await _paymentRepository.GetByTicketIdAsync(ticketId);
        if (payment != null && payment.Status == PaymentStatus.pending)
        {
            payment.Status = reason.Contains("TTL") ? PaymentStatus.expired : PaymentStatus.failed;
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

    public bool IsWithinTimeLimit(DateTime reservedAt, DateTime paymentReceivedAt)
    {
        var expirationTime = reservedAt.AddMinutes(5);
        return paymentReceivedAt <= expirationTime;
    }
}
