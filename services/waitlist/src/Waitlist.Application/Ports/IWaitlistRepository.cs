using Waitlist.Domain.Entities;

namespace Waitlist.Application.Ports;

public interface IWaitlistRepository
{
    Task AddAsync(WaitlistEntry entry, CancellationToken cancellationToken = default);
    Task<WaitlistEntry?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<bool> HasActiveEntryAsync(string email, Guid eventId, CancellationToken cancellationToken = default);
    Task<WaitlistEntry?> GetNextPendingAsync(Guid eventId, CancellationToken cancellationToken = default);
    Task<int> GetQueuePositionAsync(Guid entryId, CancellationToken cancellationToken = default);
    Task<bool> HasAssignedEntryForSeatAsync(Guid seatId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<WaitlistEntry>> GetExpiredAssignedAsync(CancellationToken cancellationToken = default);
    Task UpdateAsync(WaitlistEntry entry, CancellationToken cancellationToken = default);
}
