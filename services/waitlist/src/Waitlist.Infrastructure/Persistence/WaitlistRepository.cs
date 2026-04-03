using Microsoft.EntityFrameworkCore;
using Waitlist.Application.Ports;
using Waitlist.Domain.Entities;

namespace Waitlist.Infrastructure.Persistence;

public class WaitlistRepository : IWaitlistRepository
{
    private readonly WaitlistDbContext _db;

    public WaitlistRepository(WaitlistDbContext db)
    {
        _db = db;
    }

    public async Task AddAsync(WaitlistEntry entry, CancellationToken cancellationToken = default)
    {
        _db.WaitlistEntries.Add(entry);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task<WaitlistEntry?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => await _db.WaitlistEntries.FindAsync(new object[] { id }, cancellationToken);

    public async Task<bool> HasActiveEntryAsync(string email, Guid eventId, CancellationToken cancellationToken = default)
        => await _db.WaitlistEntries.AnyAsync(
            e => e.Email == email &&
                 e.EventId == eventId &&
                 (e.Status == WaitlistEntry.StatusPending || e.Status == WaitlistEntry.StatusAssigned),
            cancellationToken);

    public async Task<WaitlistEntry?> GetNextPendingAsync(Guid eventId, CancellationToken cancellationToken = default)
        => await _db.WaitlistEntries
            .Where(e => e.EventId == eventId && e.Status == WaitlistEntry.StatusPending)
            .OrderBy(e => e.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

    public async Task<int> GetQueuePositionAsync(Guid entryId, CancellationToken cancellationToken = default)
    {
        var entry = await _db.WaitlistEntries.FindAsync(new object[] { entryId }, cancellationToken);
        if (entry is null) return 0;

        return await _db.WaitlistEntries.CountAsync(
            e => e.EventId == entry.EventId &&
                 e.Status == WaitlistEntry.StatusPending &&
                 e.CreatedAt <= entry.CreatedAt,
            cancellationToken);
    }

    public async Task<bool> HasAssignedEntryForSeatAsync(Guid seatId, CancellationToken cancellationToken = default)
        => await _db.WaitlistEntries.AnyAsync(
            e => e.SeatId == seatId && e.Status == WaitlistEntry.StatusAssigned,
            cancellationToken);

    public async Task<IReadOnlyList<WaitlistEntry>> GetExpiredAssignedAsync(CancellationToken cancellationToken = default)
        => await _db.WaitlistEntries
            .Where(e => e.Status == WaitlistEntry.StatusAssigned && e.ExpiresAt <= DateTime.UtcNow)
            .ToListAsync(cancellationToken);

    public async Task UpdateAsync(WaitlistEntry entry, CancellationToken cancellationToken = default)
    {
        _db.WaitlistEntries.Update(entry);
        await _db.SaveChangesAsync(cancellationToken);
    }
}
