// 🛡️ ARCHITECTURE DECISION:
// Esta clase pertenece a la capa Application dentro de una arquitectura hexagonal.
// La capa Application no debe depender de detalles de infraestructura como Entity Framework.
// 
// Para evitar acoplamiento directo a excepciones de EF Core (DbUpdateConcurrencyException,
// DbUpdateException), se definieron excepciones abstractas propias del dominio de aplicación:
// - ConcurrencyException
// - DuplicateEntryException
//
// La capa Infrastructure es responsable de traducir las excepciones específicas del framework
// hacia estas abstracciones mediante un mecanismo de "exception translation".
//
// Esto garantiza:
// 1. Inversión de dependencias (DIP)
// 2. Independencia de framework
// 3. Testabilidad sin EF Core
// 4. Sustituibilidad de persistencia sin modificar Application

using System.Data;
using Microsoft.Extensions.Logging;
using PaymentService.Application.Dtos;
using PaymentService.Application.Exceptions;
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
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<ProcessPaymentApprovedUseCase> _logger;

    public ProcessPaymentApprovedUseCase(
        ITicketRepository ticketRepository,
        IPaymentRepository paymentRepository,
        ITicketHistoryRepository historyRepository,
        IUnitOfWork unitOfWork,
        ILogger<ProcessPaymentApprovedUseCase> logger)
    {
        _ticketRepository = ticketRepository;
        _paymentRepository = paymentRepository;
        _historyRepository = historyRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<ValidationResult> ExecuteAsync(PaymentApprovedEventDto paymentEvent)
    {
        try
        {
            // 1. Verificar idempotencia por ProviderRef
            var existingPayment = await _paymentRepository
                .GetByProviderRefAsync(paymentEvent.TransactionRef, CancellationToken.None);

            if (existingPayment is not null)
            {
                _logger.LogInformation(
                    "Duplicate payment event detected. ProviderRef: {ProviderRef}",
                    paymentEvent.TransactionRef);

                return ValidationResult.AlreadyProcessed();
            }

            // 2. Buscar ticket
            var ticket = await _ticketRepository
                .GetTrackedByIdAsync(paymentEvent.TicketId, CancellationToken.None);

            if (ticket is null)
            {
                _logger.LogWarning("Ticket {TicketId} not found", paymentEvent.TicketId);
                return ValidationResult.Failure("Ticket not found");
            }

            if (ticket.Status != TicketStatus.reserved)
            {
                _logger.LogWarning(
                    "Invalid ticket status for payment. TicketId: {TicketId}, Status: {Status}",
                    paymentEvent.TicketId, ticket.Status);

                return ValidationResult.Failure($"Invalid ticket status: {ticket.Status}");
            }

            // 3. Validar TTL
            if (ticket.ReservedAt is null ||
                !IsWithinTimeLimit(ticket.ReservedAt.Value, paymentEvent.ApprovedAt))
            {
                _logger.LogWarning("Payment received after TTL. TicketId: {TicketId}", paymentEvent.TicketId);
                await TransitionToReleasedAsync(paymentEvent.TicketId, "Payment received after TTL");
                return ValidationResult.Failure("TTL exceeded");
            }

            // 4. Crear payment
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

            // 5. Transicionar a pagado
            var success = await TransitionToPaidAsync(paymentEvent.TicketId, paymentEvent.TransactionRef);

            if (!success)
            {
                _logger.LogWarning("Failed to transition ticket {TicketId} to paid", paymentEvent.TicketId);
                return ValidationResult.Failure("State transition failed");
            }

            _logger.LogInformation("Payment processed successfully for ticket {TicketId}", paymentEvent.TicketId);
            return ValidationResult.Success();
        }
        catch (DuplicateEntryException dbEx)
        {
            _logger.LogWarning(dbEx,
                "Duplicate provider reference detected: {ProviderRef}",
                paymentEvent.TransactionRef);

            return ValidationResult.AlreadyProcessed();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error processing approved payment for ticket {TicketId}",
                paymentEvent.TicketId);

            throw;
        }
    }

    private async Task<bool> TransitionToPaidAsync(long ticketId, string providerRef)
    {
        await using var tx = await _unitOfWork
            .BeginTransactionAsync(IsolationLevel.RepeatableRead);

        try
        {
            var ticket = await _ticketRepository
                .GetTrackedByIdAsync(ticketId, CancellationToken.None);

            if (ticket is null || ticket.Status != TicketStatus.reserved)
                return false;

            var oldStatus = ticket.Status;

            ticket.Status = TicketStatus.paid;
            ticket.PaidAt = DateTime.UtcNow;

            var payment = ticket.Payments.FirstOrDefault();

            if (payment is not null)
            {
                payment.Status = PaymentStatus.approved;
                payment.ProviderRef = providerRef;
                payment.UpdatedAt = DateTime.UtcNow;
            }

            _historyRepository.Add(new TicketHistory
            {
                TicketId = ticketId,
                OldStatus = oldStatus,
                NewStatus = TicketStatus.paid,
                ChangedAt = DateTime.UtcNow,
                Reason = "Payment approved"
            });

            await _unitOfWork.SaveChangesAsync();
            await tx.CommitAsync();

            _logger.LogInformation(
                "Ticket {TicketId} successfully transitioned to Paid with provider ref {ProviderRef}",
                ticketId, providerRef);

            return true;
        }
        catch (ConcurrencyException)
        {
            await tx.RollbackAsync();
            return false;
        }
    }

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

            // Idempotencia
            if (ticket.Status == TicketStatus.available)
            {
                _logger.LogInformation("Ticket {TicketId} already available (idempotent event).", ticketId);
                return true;
            }

            var oldStatus = ticket.Status;

            ticket.Status = TicketStatus.available;
            ticket.ExpiresAt = null;

            var payment = ticket.Payments
                .FirstOrDefault(p => p.Status == PaymentStatus.pending);

            if (payment is not null)
            {
                payment.Status = reason.Contains("TTL", StringComparison.OrdinalIgnoreCase)
                    ? PaymentStatus.expired
                    : PaymentStatus.failed;

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
            _logger.LogWarning("Concurrency conflict while releasing ticket {TicketId}", ticketId);
            return false;
        }
        catch (Exception ex)
        {
            await tx.RollbackAsync();
            _logger.LogError(ex, "Error transitioning ticket {TicketId} to released", ticketId);
            throw;
        }
    }

    public bool IsWithinTimeLimit(DateTime reservedAt, DateTime paymentReceivedAt)
    {
        var expirationTime = reservedAt.AddMinutes(5);
        return paymentReceivedAt <= expirationTime;
    }
}
