using Microsoft.EntityFrameworkCore;
using MsPaymentService.Worker.Data;
using MsPaymentService.Worker.Models.Entities;

namespace MsPaymentService.Worker.Repositories;

/// <summary>
/// Implementación del repositorio de tickets usando Entity Framework Core.
/// Proporciona acceso a datos con soporte para concurrencia optimista y bloqueo pesimista.
/// </summary>
public class TicketRepository : ITicketRepository
{
    private readonly PaymentDbContext _context;

    /// <summary>
    /// Inicializa una nueva instancia de <see cref="TicketRepository"/>.
    /// </summary>
    /// <param name="context">Contexto de base de datos de Entity Framework.</param>
    public TicketRepository(PaymentDbContext context)
    {
        _context = context;
    }

    /// <inheritdoc/>
    public async Task<Ticket?> GetByIdAsync(long id)
    {
        return await _context.Tickets
            .Include(t => t.Payments)
            .FirstOrDefaultAsync(t => t.Id == id);
    }

    /// <inheritdoc/>
    public async Task<List<Ticket>> GetExpiredReservedTicketsAsync(DateTime expirationThreshold)
    {
        return await _context.Tickets
            .Where(t => t.Status == TicketStatus.reserved && 
                       t.ExpiresAt != null && 
                       t.ExpiresAt < expirationThreshold)
            .ToListAsync();
    }

    public async Task<Ticket?> GetTrackedByIdAsync(long id, CancellationToken ct)
    {
        return await _context.Tickets
            .Include(t => t.Payments)
            .Include(t => t.History)
            .FirstOrDefaultAsync(t => t.Id == id, ct);
    }
}