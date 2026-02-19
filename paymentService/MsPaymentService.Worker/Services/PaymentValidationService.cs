using Microsoft.EntityFrameworkCore;
using MsPaymentService.Worker.Models.DTOs;
using MsPaymentService.Worker.Models.Entities;
using MsPaymentService.Worker.Models.Events;
using MsPaymentService.Worker.Repositories;

namespace MsPaymentService.Worker.Services;

public class PaymentValidationService : IPaymentValidationService
{
    private readonly ITicketRepository _ticketRepository;
    private readonly IPaymentRepository _paymentRepository;
    private readonly ITicketStateService _stateService;
    private readonly ILogger<PaymentValidationService> _logger;

    public PaymentValidationService(
        ITicketRepository ticketRepository,
        IPaymentRepository paymentRepository,
        ITicketStateService stateService,
        ILogger<PaymentValidationService> logger)
    {
        _ticketRepository = ticketRepository;
        _paymentRepository = paymentRepository;
        _stateService = stateService;
        _logger = logger;
    }

    public async Task<ValidationResult> ValidateAndProcessApprovedPaymentAsync(PaymentApprovedEvent paymentEvent)
    {
        try
        {
            // 1. Verificar idempotencia
            var existingPayment = await _paymentRepository
                .GetByProviderRefAsync(paymentEvent.TransactionRef, CancellationToken.None);
            
            if (existingPayment is not null)
            {
                _logger.LogInformation(
                    "Duplicate payment event detected. ProviderRef: {ProviderRef}",
                    paymentEvent.TransactionRef);

                return ValidationResult.AlreadyProcessed();
            }

            var ticket = await _ticketRepository
                .GetTrackedByIdAsync(paymentEvent.TicketId, CancellationToken.None);


            if (ticket is null)
            {
                _logger.LogWarning(
                    "Ticket {TicketId} not found",
                    paymentEvent.TicketId);

                return ValidationResult.Failure("Ticket not found");
            }


            if (ticket.Status != TicketStatus.reserved)
            {
                _logger.LogWarning(
                    "Invalid ticket status for payment. TicketId: {TicketId}, Status: {Status}",
                    paymentEvent.TicketId,
                    ticket.Status);

                return ValidationResult.Failure($"Invalid ticket status: {ticket.Status}");
            }

            // 3. Validar TTL
            if (ticket.ReservedAt is null ||
                !IsWithinTimeLimit(ticket.ReservedAt.Value, paymentEvent.ApprovedAt))
            {
                _logger.LogWarning(
                    "Payment received after TTL. TicketId: {TicketId}",
                    paymentEvent.TicketId);

                await _stateService.TransitionToReleasedAsync(
                    paymentEvent.TicketId,
                    "Payment received after TTL");

                return ValidationResult.Failure("TTL exceeded");
            }

            // 4. Obtener o crear payment
            await _paymentRepository.AddAsync(new Payment
            {
                TicketId = paymentEvent.TicketId,
                Status = PaymentStatus.pending,
                AmountCents = paymentEvent.AmountCents,
                Currency = paymentEvent.Currency,
                ProviderRef = paymentEvent.TransactionRef,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            }, CancellationToken.None);

            // 5. Procesar transición exitosa
            var success = await _stateService.TransitionToPaidAsync(
                paymentEvent.TicketId,
                paymentEvent.TransactionRef);
            
            if (!success)
            {
                _logger.LogWarning(
                    "Failed to transition ticket {TicketId} to paid",
                    paymentEvent.TicketId);

                return ValidationResult.Failure("State transition failed");
            }

            _logger.LogInformation(
                "Payment processed successfully for ticket {TicketId}",
                paymentEvent.TicketId);

            return ValidationResult.Success();
        }
        catch (DbUpdateException dbEx)
        {
            // Probable violación de unique index (idempotencia)
            _logger.LogWarning(
                dbEx,
                "Duplicate provider reference detected: {ProviderRef}",
                paymentEvent.TransactionRef);

            return ValidationResult.AlreadyProcessed();
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error processing approved payment for ticket {TicketId}",
                paymentEvent.TicketId);

            throw;
        }
    }

    public async Task<ValidationResult> ValidateAndProcessRejectedPaymentAsync(PaymentRejectedEvent paymentEvent)
    {
        try
        {
            var success = await _stateService.TransitionToReleasedAsync(
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
            _logger.LogError(
                ex,
                "Error processing rejected payment for ticket {TicketId}",
                paymentEvent.TicketId);

            throw;
        }
    }

    public bool IsWithinTimeLimit(DateTime reservedAt, DateTime paymentReceivedAt)
    {
        var expirationTime = reservedAt.AddMinutes(5);
        return paymentReceivedAt <= expirationTime;
    }
}