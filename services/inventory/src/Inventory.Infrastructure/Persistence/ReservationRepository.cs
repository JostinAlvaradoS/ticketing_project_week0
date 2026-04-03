using Inventory.Application.Ports;
using Inventory.Domain.Entities;
using Microsoft.EntityFrameworkCore;

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

    public async Task<Reservation?> GetByIdAsync(Guid reservationId, CancellationToken cancellationToken)
    {
        return await _context.Reservations
            .FirstOrDefaultAsync(r => r.Id == reservationId, cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task UpdateAsync(Reservation reservation, CancellationToken cancellationToken)
    {
        _context.Reservations.Update(reservation);
        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}
