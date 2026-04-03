using Inventory.Domain.Entities;

namespace Inventory.Application.Ports;

/// <summary>
/// Puerto de persistencia para la entidad Seat.
/// </summary>
public interface ISeatRepository
{
    Task<Seat?> GetByIdAsync(Guid seatId, CancellationToken cancellationToken);
    Task UpdateAsync(Seat seat, CancellationToken cancellationToken);
}
