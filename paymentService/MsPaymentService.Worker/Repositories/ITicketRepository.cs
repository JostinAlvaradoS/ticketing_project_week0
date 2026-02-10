using PaymentService.Api.Models.Entities;

namespace PaymentService.Api.Repositories;

public interface ITicketRepository
{
    Task<Ticket?> GetByIdAsync(long id);
    Task<Ticket?> GetByIdForUpdateAsync(long id); // SELECT FOR UPDATE
    Task<bool> UpdateAsync(Ticket ticket);
    Task<List<Ticket>> GetExpiredReservedTicketsAsync(DateTime expirationThreshold);
}