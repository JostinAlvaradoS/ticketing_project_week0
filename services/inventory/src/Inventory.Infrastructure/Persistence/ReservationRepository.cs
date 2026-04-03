using Inventory.Application.Ports;
using Inventory.Domain.Entities;

namespace Inventory.Infrastructure.Persistence;

public class ReservationRepository : IReservationRepository
{
    private readonly InventoryDbContext _context;

    public ReservationRepository(InventoryDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task<Reservation> CreateAsync(Reservation reservation, CancellationToken cancellationToken)
    {
        _context.Reservations.Add(reservation);
        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return reservation;
    }
}
