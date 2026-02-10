using PaymentService.Api.Models.Entities;

namespace PaymentService.Api.Repositories;

public interface ITicketHistoryRepository
{
    Task AddAsync(TicketHistory history);
    Task<List<TicketHistory>> GetByTicketIdAsync(long ticketId);
}