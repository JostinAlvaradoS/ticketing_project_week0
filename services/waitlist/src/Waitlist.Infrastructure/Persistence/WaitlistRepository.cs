using Microsoft.EntityFrameworkCore;
using Waitlist.Application.Ports;
using Waitlist.Domain.Entities;

namespace Waitlist.Infrastructure.Persistence;

public class WaitlistRepository : IWaitlistRepository
{
    private readonly WaitlistDbContext _ctx;

    public WaitlistRepository(WaitlistDbContext ctx) => _ctx = ctx;

    public async Task AddAsync(WaitlistEntry entry, CancellationToken cancellationToken = default)
    {
        _ctx.WaitlistEntries.Add(entry);
        await _ctx.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(WaitlistEntry entry, CancellationToken cancellationToken = default)
    {
        _ctx.WaitlistEntries.Update(entry);
        await _ctx.SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> HasActiveEntryAsync(string email, Guid eventId, CancellationToken cancellationToken = default) =>
        await _ctx.WaitlistEntries.AnyAsync(e =>
            e.Email == email &&
            e.EventId == eventId &&
            (e.Status == WaitlistEntry.StatusPending || e.Status == WaitlistEntry.StatusAssigned),
            cancellationToken);

    public async Task<WaitlistEntry?> GetNextPendingAsync(Guid eventId, CancellationToken cancellationToken = default) =>
        await _ctx.WaitlistEntries
            .Where(e => e.EventId == eventId && e.Status == WaitlistEntry.StatusPending)
            .OrderBy(e => e.RegisteredAt)
            .FirstOrDefaultAsync(cancellationToken);

    public async Task<WaitlistEntry?> GetByOrderIdAsync(Guid orderId, CancellationToken cancellationToken = default) =>
        await _ctx.WaitlistEntries
            .FirstOrDefaultAsync(e => e.OrderId == orderId, cancellationToken);

    public async Task<List<WaitlistEntry>> GetExpiredAssignedAsync(CancellationToken cancellationToken = default) =>
        await _ctx.WaitlistEntries
            .Where(e => e.Status == WaitlistEntry.StatusAssigned && e.ExpiresAt <= DateTime.UtcNow)
            .ToListAsync(cancellationToken);

    public async Task<int> GetQueuePositionAsync(Guid eventId, CancellationToken cancellationToken = default) =>
        await _ctx.WaitlistEntries
            .CountAsync(e => e.EventId == eventId && e.Status == WaitlistEntry.StatusPending, cancellationToken);

    public async Task<bool> HasAssignedEntryForSeatAsync(Guid seatId, CancellationToken cancellationToken = default) =>
        await _ctx.WaitlistEntries
            .AnyAsync(e => e.SeatId == seatId && e.Status == WaitlistEntry.StatusAssigned, cancellationToken);
}
