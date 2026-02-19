using Microsoft.EntityFrameworkCore;
using PaymentService.Application.Ports.Outbound;
using PaymentService.Domain.Entities;
using PaymentService.Infrastructure.Persistence;

namespace PaymentService.Infrastructure.Persistence;

public class TicketHistoryRepository : ITicketHistoryRepository
{
    private readonly PaymentDbContext _dbContext;

    public TicketHistoryRepository(PaymentDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(TicketHistory history)
    {
        _dbContext.TicketHistory.Add(history);
        await _dbContext.SaveChangesAsync();
    }

    public async Task<List<TicketHistory>> GetByTicketIdAsync(long ticketId)
    {
        return await _dbContext.TicketHistory
            .Where(h => h.TicketId == ticketId)
            .OrderBy(h => h.ChangedAt)
            .ToListAsync();
    }
}
