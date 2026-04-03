using Inventory.Domain.Entities;

namespace Inventory.Application.Ports;

/// <summary>
/// Puerto de persistencia para la entidad Reservation.
/// </summary>
public interface IReservationRepository
{
    Task<Reservation> CreateAsync(Reservation reservation, CancellationToken cancellationToken);
}
