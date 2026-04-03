namespace Inventory.Application.Ports;

/// <summary>
/// Puerto para adquisición y liberación de locks distribuidos (Redis).
/// </summary>
public interface IRedisLock
{
    /// <summary>
    /// Attempts to acquire a lock for the provided key. Returns a lock token if acquired, otherwise null.
    /// </summary>
    Task<string?> AcquireLockAsync(string key, TimeSpan ttl);

    /// <summary>
    /// Releases the lock if the token matches. Returns true if released.
    /// </summary>
    Task<bool> ReleaseLockAsync(string key, string token);
}
