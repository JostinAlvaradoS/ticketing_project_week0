namespace Inventory.Application.Ports;

/// <summary>
/// Port for consulting the Waitlist Service.
/// ADR-03: Before releasing a seat, the worker must check if there is an active waitlist queue.
/// </summary>
public interface IWaitlistClient
{
    /// <summary>
    /// Returns true if there is at least one pending waitlist entry for the given event.
    /// </summary>
    Task<bool> HasPendingAsync(Guid eventId, CancellationToken cancellationToken = default);
}
