using Microsoft.EntityFrameworkCore;
using PaymentService.Application.Ports.Outbound;
using PaymentService.Domain.Entities;
using PaymentService.Domain.Enums;
using PaymentService.Infrastructure.Persistence;

namespace PaymentService.Infrastructure.Persistence;

public class TicketRepository : ITicketRepository
{
    private readonly PaymentDbContext _dbContext;

    public TicketRepository(PaymentDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Ticket?> GetByIdAsync(long id)
    {
        return await _dbContext.Tickets
            .Include(t => t.Payments)
            .FirstOrDefaultAsync(t => t.Id == id);
    }

    public async Task<List<Ticket>> GetExpiredReservedTicketsAsync(DateTime expirationThreshold)
    {
        return await _dbContext.Tickets
            .Where(t => t.Status == TicketStatus.reserved && 
                       t.ExpiresAt != null && 
                       t.ExpiresAt < expirationThreshold)
            .ToListAsync();
    }

    public async Task<Ticket?> GetTrackedByIdAsync(long id, CancellationToken ct)
    {
        return await _dbContext.Tickets
            .Include(t => t.Payments)
            .Include(t => t.History)
            .FirstOrDefaultAsync(t => t.Id == id, ct);
    }
}
