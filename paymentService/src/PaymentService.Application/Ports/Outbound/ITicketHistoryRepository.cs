using PaymentService.Domain.Entities;

namespace PaymentService.Application.Ports.Outbound;

public interface ITicketHistoryRepository
{
    void Add(TicketHistory history);
    Task<List<TicketHistory>> GetByTicketIdAsync(long ticketId);
}
