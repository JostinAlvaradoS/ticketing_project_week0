using Microsoft.EntityFrameworkCore;
using Fulfillment.Domain.Entities;
using Fulfillment.Application.Ports;

namespace Fulfillment.Infrastructure.Persistence;

public class TicketRepository : ITicketRepository
{
    private readonly FulfillmentDbContext _context;

    public TicketRepository(FulfillmentDbContext context)
    {
        _context = context;
    }

    public async Task<Ticket?> GetByOrderIdAsync(Guid orderId)
    {
        return await _context.Tickets.FirstOrDefaultAsync(t => t.OrderId == orderId);
    }

    public async Task<Ticket?> GetByIdAsync(Guid id)
    {
        return await _context.Tickets.FirstOrDefaultAsync(t => t.Id == id);
    }

    public async Task<Ticket> CreateAsync(Ticket ticket)
    {
        ticket.CreatedAt = DateTime.UtcNow;
        ticket.UpdatedAt = DateTime.UtcNow;
        
        var entry = await _context.Tickets.AddAsync(ticket);
        await _context.SaveChangesAsync();
        
        return entry.Entity;
    }

    public async Task<Ticket> UpdateAsync(Ticket ticket)
    {
        ticket.UpdatedAt = DateTime.UtcNow;
        _context.Tickets.Update(ticket);
        await _context.SaveChangesAsync();
        
        return ticket;
    }

    public async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }
}
