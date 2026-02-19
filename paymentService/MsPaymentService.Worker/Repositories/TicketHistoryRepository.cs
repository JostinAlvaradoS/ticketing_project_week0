using Microsoft.EntityFrameworkCore;
using MsPaymentService.Worker.Data;
using MsPaymentService.Worker.Models.Entities;

namespace MsPaymentService.Worker.Repositories;

public class TicketHistoryRepository : ITicketHistoryRepository
{
    private readonly PaymentDbContext _context;

    public TicketHistoryRepository(PaymentDbContext context)
    {
        _context = context;
    }

    public void Add(TicketHistory history)
    {
        _context.TicketHistory.Add(history);
    }

    public async Task<List<TicketHistory>> GetByTicketIdAsync(long ticketId)
    {
        return await _context.TicketHistory
            .Where(h => h.TicketId == ticketId)
            .OrderBy(h => h.ChangedAt)
            .ToListAsync();
    }
}