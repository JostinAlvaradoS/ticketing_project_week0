using System.Data;
using Microsoft.EntityFrameworkCore;
using MsPaymentService.Worker.Data;
using MsPaymentService.Worker.Models.Entities;
using MsPaymentService.Worker.Repositories;

namespace MsPaymentService.Worker.Services;

public class TicketStateService : ITicketStateService
{
    private readonly ITicketRepository _ticketRepository;
    private readonly IPaymentRepository _paymentRepository;
    private readonly ITicketHistoryRepository _historyRepository;
    private readonly ILogger<TicketStateService> _logger;
    private readonly IUnitOfWork _unitOfWork;

    public TicketStateService(
        ITicketRepository ticketRepository,
        IPaymentRepository paymentRepository,
        ITicketHistoryRepository historyRepository,
        ILogger<TicketStateService> logger,
        IUnitOfWork unitOfWork)
    {
        _ticketRepository = ticketRepository;
        _paymentRepository = paymentRepository;
        _historyRepository = historyRepository;
        _logger = logger;
        _unitOfWork = unitOfWork;
    }

    /// <summary>
    /// Transiciona un ticket del estado 'reservado' al estado 'pagado'.
    /// Esta operación es atómica y maneja concurrencia mediante transacciones con nivel RepeatableRead.
    /// </summary>
    /// <param name="ticketId">Identificador único del ticket a transicionar.</param>
    /// <param name="providerRef">Referencia del proveedor de pago que aprobó la transacción.</param>
    /// <returns>
    /// True si la transición fue exitosa, False si:
    /// - El ticket no existe
    /// - El ticket no está en estado 'reservado'
    /// - Ocurrió un conflicto de concurrencia
    /// </returns>
    /// <remarks>
    /// Esta operación actualiza:
    /// - El estado del ticket a 'paid'
    /// - La fecha de pago (PaidAt)
    /// - El estado del pago asociado a 'approved'
    /// - La referencia del proveedor en el pago
    /// - Registra una entrada en el historial del ticket
    /// </remarks>
    public async Task<bool> TransitionToPaidAsync(long ticketId, string providerRef)
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

            if (payment != null)
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

            return true;
        }
        catch (DbUpdateConcurrencyException)
        {
            await tx.RollbackAsync();
            return false;
        }
    }

    /// <summary>
    /// Transiciona un ticket al estado 'disponible' (liberado), cancelando cualquier reserva activa.
    /// Esta operación es idempotente y maneja concurrencia mediante transacciones.
    /// </summary>
    /// <param name="ticketId">Identificador único del ticket a liberar.</param>
    /// <param name="reason">Motivo de la liberación (ej: "TTL expired", "Payment failed").</param>
    /// <returns>
    /// True si la transición fue exitosa, False si:
    /// - El ticket no existe
    /// - Ocurrió un conflicto de concurrencia
    /// </returns>
    /// <remarks>
    /// Esta operación es idempotente: si el ticket ya está disponible, retorna True sin realizar cambios.
    /// 
    /// Actualiza:
    /// - El estado del ticket a 'available'
    /// - Limpia la fecha de expiración (ExpiresAt = null)
    /// - Actualiza el estado del pago pendiente basado en la razón:
    ///   * 'expired' si la razón contiene "TTL"
    ///   * 'failed' en otros casos
    /// - Registra una entrada en el historial del ticket
    /// </remarks>
    public async Task<bool> TransitionToReleasedAsync(long ticketId, string reason)
    {
        await using var tx = await _unitOfWork
            .BeginTransactionAsync(IsolationLevel.RepeatableRead);
        
        try
        {
            var ticket = await _ticketRepository
                .GetTrackedByIdAsync(ticketId, CancellationToken.None);  

            if (ticket is null)
            {
                _logger.LogWarning(
                    "Ticket {TicketId} not found",
                    ticketId);

                return false;
            }

            // Idempotencia fuerte
            if (ticket.Status == TicketStatus.available)
            {
                _logger.LogInformation(
                    "Ticket {TicketId} already available (idempotent event).",
                    ticketId);

                return true;
            }
            
            var oldStatus = ticket.Status;

            ticket.Status = TicketStatus.available;
            ticket.ExpiresAt = null;

            // Payment ya viene trackeado si usas Include
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
                ticketId,
                reason);

            return true;
        }
        catch (DbUpdateConcurrencyException)
        {
            await tx.RollbackAsync();

            _logger.LogWarning(
                "Concurrency conflict while releasing ticket {TicketId}",
                ticketId);

            return false;
        }
        catch (Exception ex)
        {
            await tx.RollbackAsync();

            _logger.LogError(
                ex,
                "Error transitioning ticket {TicketId} to released",
                ticketId);

            throw;
        }
    }

    /// <summary>
    /// Registra una entrada de historial para documentar un cambio de estado en un ticket.
    /// Esta operación no realiza commit automático - debe ser llamada dentro de una transacción.
    /// </summary>
    /// <param name="ticketId">Identificador único del ticket cuyo estado cambió.</param>
    /// <param name="oldStatus">Estado anterior del ticket antes del cambio.</param>
    /// <param name="newStatus">Nuevo estado al cual transicionó el ticket.</param>
    /// <param name="reason">Motivo o descripción del cambio de estado.</param>
    /// <remarks>
    /// Este método solo agrega la entrada al contexto de Entity Framework.
    /// El guardado debe realizarse explícitamente mediante SaveChangesAsync.
    /// La fecha de cambio se establece automáticamente como DateTime.UtcNow.
    /// </remarks>
    public async Task RecordHistoryAsync(long ticketId, TicketStatus oldStatus, TicketStatus newStatus, string reason)
    {
        var history = new TicketHistory
        {
            TicketId = ticketId,
            OldStatus = oldStatus,
            NewStatus = newStatus,
            ChangedAt = DateTime.UtcNow,
            Reason = reason
        };
        
        _historyRepository.Add(history);
        
        _logger.LogDebug(
            "Recorded history for ticket {TicketId}: {OldStatus} -> {NewStatus}. Reason: {Reason}",
            ticketId, oldStatus, newStatus, reason);
    }
}