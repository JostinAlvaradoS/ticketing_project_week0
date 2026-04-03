using Inventory.Application.Ports;
using Inventory.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Inventory.Infrastructure.Persistence;

public class SeatRepository : ISeatRepository
{
    private readonly InventoryDbContext _context;

    public SeatRepository(InventoryDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task<Seat?> GetByIdAsync(Guid seatId, CancellationToken cancellationToken)
    {
        return await _context.Seats.FindAsync([seatId], cancellationToken: cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task UpdateAsync(Seat seat, CancellationToken cancellationToken)
    {
        _context.Seats.Update(seat);
        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}
