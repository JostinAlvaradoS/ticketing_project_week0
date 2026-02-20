using PaymentService.Domain.Entities;

namespace PaymentService.Application.Ports.Outbound;

public interface ITicketRepository
{
    Task<Ticket?> GetByIdAsync(long id);
    Task<List<Ticket>> GetExpiredReservedTicketsAsync(DateTime expirationThreshold);
    Task<Ticket?> GetTrackedByIdAsync(long id, CancellationToken ct);
}
