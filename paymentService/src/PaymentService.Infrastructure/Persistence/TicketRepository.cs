using Microsoft.EntityFrameworkCore;
using PaymentService.Application.Ports.Outbound;
using PaymentService.Domain.Entities;
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

    public async Task<Ticket?> GetByIdForUpdateAsync(long id)
    {
        return await _dbContext.Tickets
            .FromSqlRaw("SELECT * FROM public.tickets WHERE id = {0} FOR UPDATE", id)
            .FirstOrDefaultAsync();
    }

    public async Task<bool> UpdateAsync(Ticket ticket)
    {
        _dbContext.Tickets.Update(ticket);
        try
        {
            await _dbContext.SaveChangesAsync();
            return true;
        }
        catch (DbUpdateConcurrencyException)
        {
            return false;
        }
    }

    public async Task<List<Ticket>> GetExpiredReservedTicketsAsync(DateTime expirationThreshold)
    {
        return await _dbContext.Tickets
            .Where(t => t.Status == Domain.Enums.TicketStatus.reserved && t.ExpiresAt < expirationThreshold)
            .ToListAsync();
    }
}
