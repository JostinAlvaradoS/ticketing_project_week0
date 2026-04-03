using Inventory.Domain.Entities;

namespace Inventory.Application.Ports;

/// <summary>
/// Puerto de persistencia para la entidad Reservation.
/// </summary>
public interface IReservationRepository
{
    Task<Reservation> CreateAsync(Reservation reservation, CancellationToken cancellationToken);
    Task<Reservation?> GetByIdAsync(Guid reservationId, CancellationToken cancellationToken);
    Task UpdateAsync(Reservation reservation, CancellationToken cancellationToken);
}
