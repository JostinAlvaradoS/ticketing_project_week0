using CrudService.Domain.Entities;

namespace CrudService.Application.Ports.Outbound;

public interface ITicketHistoryRepository
{
    Task<IEnumerable<TicketHistory>> GetByTicketIdAsync(long ticketId);
    Task<TicketHistory> AddAsync(TicketHistory history);
    Task SaveChangesAsync();
}
