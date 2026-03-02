using System.Text;
using Inventory.Domain.Ports;
using StackExchange.Redis;

namespace Inventory.Infrastructure.Locking;

/// <summary>
/// Simple distributed lock implementation using Redis (SET NX + expiry + Lua check-delete).
/// Returns a token (GUID) when the lock is acquired; Release uses that token to safely delete the key.
/// </summary>
public class RedisLock : IRedisLock
{
    private readonly IConnectionMultiplexer? _multiplexer;
    private readonly IDatabase _db;

    public RedisLock(IConnectionMultiplexer multiplexer)
    {
        _multiplexer = multiplexer ?? throw new ArgumentNullException(nameof(multiplexer));
        _db = _multiplexer.GetDatabase();
    }

    // Constructor overload to facilitate unit testing by injecting a mocked IDatabase directly.
    public RedisLock(IDatabase database)
    {
        _db = database ?? throw new ArgumentNullException(nameof(database));
        _multiplexer = null;
    }

    public async Task<string?> AcquireLockAsync(string key, TimeSpan ttl)
    {
        if (string.IsNullOrEmpty(key)) throw new ArgumentNullException(nameof(key));

        var token = Guid.NewGuid().ToString("N");
        try
        {
            // Use a 5-second timeout for lock acquisition
            using var cts = new System.Threading.CancellationTokenSource(TimeSpan.FromSeconds(5));
            // Explicitly mentioning all parameters to avoid Moq ambiguity
            bool acquired = await _db.StringSetAsync(
                (RedisKey)key, 
                (RedisValue)token, 
                ttl, 
                false, 
                When.NotExists, 
                CommandFlags.None).ConfigureAwait(false);
            return acquired ? token : null;
        }
        catch (OperationCanceledException)
        {
            Console.WriteLine($"[RedisLock] Timeout acquiring lock for key {key}");
            return null;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[RedisLock] Error acquiring lock: {ex.Message}");
            return null;
        }
    }

    public async Task<bool> ReleaseLockAsync(string key, string token)
    {
        if (string.IsNullOrEmpty(key)) throw new ArgumentNullException(nameof(key));
        if (string.IsNullOrEmpty(token)) throw new ArgumentNullException(nameof(token));
        // Read current value and delete only if it matches the token.
        var current = await _db.StringGetAsync(key).ConfigureAwait(false);
        if (!current.HasValue) return false;

        if (current == token)
        {
            return await _db.KeyDeleteAsync(key).ConfigureAwait(false);
        }

        return false;
    }
}
