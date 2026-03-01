using Fulfillment.Domain.Entities;

namespace Fulfillment.Application.Ports;

public interface ITicketRepository
{
    Task<Ticket?> GetByIdAsync(Guid id);
    Task<Ticket?> GetByOrderIdAsync(Guid orderId);
    Task<Ticket> CreateAsync(Ticket ticket);
    Task<Ticket> UpdateAsync(Ticket ticket);
    Task SaveChangesAsync();
}
