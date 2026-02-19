using ReservationService.Domain.Entities;
using ReservationService.Domain.Enums;

namespace ReservationService.Application.Ports.Outbound;

public interface ITicketRepository
{
    Task<Ticket?> GetByIdAsync(long ticketId, CancellationToken cancellationToken = default);
    Task<bool> TryReserveAsync(Ticket ticket, string reservedBy, string orderId, DateTime expiresAt, CancellationToken cancellationToken = default);
}
