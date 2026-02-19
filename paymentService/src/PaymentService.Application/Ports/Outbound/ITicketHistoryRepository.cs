using PaymentService.Domain.Entities;

namespace PaymentService.Application.Ports.Outbound;

public interface ITicketHistoryRepository
{
    Task AddAsync(TicketHistory history);
    Task<List<TicketHistory>> GetByTicketIdAsync(long ticketId);
}
