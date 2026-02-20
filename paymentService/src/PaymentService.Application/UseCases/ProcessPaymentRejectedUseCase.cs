

using System.Data;
using Microsoft.Extensions.Logging;
using PaymentService.Application.Dtos;
using PaymentService.Application.Exceptions;
using PaymentService.Application.Ports.Inbound;
using PaymentService.Application.Ports.Outbound;
using PaymentService.Domain.Entities;
using PaymentService.Domain.Enums;

namespace PaymentService.Application.UseCases;

public class ProcessPaymentRejectedUseCase : IProcessPaymentRejectedUseCase
{
    private readonly ITicketRepository _ticketRepository;
    private readonly ITicketHistoryRepository _historyRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<ProcessPaymentRejectedUseCase> _logger;

    public ProcessPaymentRejectedUseCase(
        ITicketRepository ticketRepository,
        ITicketHistoryRepository historyRepository,
        IUnitOfWork unitOfWork,
        ILogger<ProcessPaymentRejectedUseCase> logger)
    {
        _ticketRepository = ticketRepository;
        _historyRepository = historyRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<ValidationResult> ExecuteAsync(PaymentRejectedEventDto paymentEvent)
    {
        try
        {
            var success = await TransitionToReleasedAsync(
                paymentEvent.TicketId,
                $"Payment rejected: {paymentEvent.RejectionReason}");

            if (!success)
            {
                return ValidationResult.AlreadyProcessed();
            }

            _logger.LogInformation(
                "Payment rejection processed for ticket {TicketId}",
                paymentEvent.TicketId);

            return ValidationResult.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error processing rejected payment for ticket {TicketId}",
                paymentEvent.TicketId);

            throw;
        }
    }

    /// <summary>
    /// Transiciona un ticket al estado 'available' (liberado), cancelando cualquier reserva activa.
    /// Operación idempotente con transacción RepeatableRead.
    /// </summary>
    private async Task<bool> TransitionToReleasedAsync(long ticketId, string reason)
    {
        await using var tx = await _unitOfWork
            .BeginTransactionAsync(IsolationLevel.RepeatableRead);

        try
        {
            var ticket = await _ticketRepository
                .GetTrackedByIdAsync(ticketId, CancellationToken.None);

            if (ticket is null)
            {
                _logger.LogWarning("Ticket {TicketId} not found", ticketId);
                return false;
            }

            // Idempotencia fuerte
            if (ticket.Status == TicketStatus.available)
            {
                _logger.LogInformation(
                    "Ticket {TicketId} already available (idempotent event).", ticketId);
                return true;
            }

            var oldStatus = ticket.Status;

            ticket.Status = TicketStatus.available;
            ticket.ExpiresAt = null;

            var payment = ticket.Payments
                .FirstOrDefault(p => p.Status == PaymentStatus.pending);

            if (payment is not null)
            {
                payment.Status = PaymentStatus.failed;
                payment.UpdatedAt = DateTime.UtcNow;
            }

            _historyRepository.Add(new TicketHistory
            {
                TicketId = ticketId,
                OldStatus = oldStatus,
                NewStatus = TicketStatus.released,
                ChangedAt = DateTime.UtcNow,
                Reason = reason
            });

            await _unitOfWork.SaveChangesAsync();
            await tx.CommitAsync();

            _logger.LogInformation(
                "Ticket {TicketId} successfully transitioned to Released. Reason: {Reason}",
                ticketId, reason);

            return true;
        }
        catch (ConcurrencyException)
        {
            await tx.RollbackAsync();

            _logger.LogWarning(
                "Concurrency conflict while releasing ticket {TicketId}", ticketId);

            return false;
        }
        catch (Exception ex)
        {
            await tx.RollbackAsync();

            _logger.LogError(ex,
                "Error transitioning ticket {TicketId} to released", ticketId);

            throw;
        }
    }
}
