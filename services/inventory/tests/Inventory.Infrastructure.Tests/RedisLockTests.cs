using System;
using System.Threading.Tasks;
using StackExchange.Redis;
using Inventory.Infrastructure.Locking;
using Xunit;

namespace Inventory.Infrastructure.Tests;

/// <summary>
/// Unit tests for RedisLock using a custom stub implementation.
/// Avoids Moq complexity with optional parameters and method overloads.
/// </summary>
public class RedisLockTests
{
    private class StubDatabase : IDatabase
    {
        private readonly bool _setAsyncReturnValue;
        private readonly RedisValue _getAsyncReturnValue;
        private readonly bool _keyDeleteAsyncReturnValue;

        public StubDatabase(bool setAsyncReturnValue = false, RedisValue? getAsyncReturnValue = null, bool keyDeleteAsyncReturnValue = false)
        {
            _setAsyncReturnValue = setAsyncReturnValue;
            _getAsyncReturnValue = getAsyncReturnValue ?? RedisValue.Null;
            _keyDeleteAsyncReturnValue = keyDeleteAsyncReturnValue;
        }

        public Task<bool> StringSetAsync(RedisKey key, RedisValue value, TimeSpan? expiry = null, When when = When.Always, CommandFlags flags = CommandFlags.None)
        {
            return Task.FromResult(_setAsyncReturnValue);
        }

        public Task<RedisValue> StringGetAsync(RedisKey key, CommandFlags flags = CommandFlags.None)
        {
            return Task.FromResult(_getAsyncReturnValue);
        }

        public Task<bool> KeyDeleteAsync(RedisKey key, CommandFlags flags = CommandFlags.None)
        {
            return Task.FromResult(_keyDeleteAsyncReturnValue);
        }

        // Stubs for all other interface members (throw NotImplementedExc if called)
        public int Database => 0;
        public IConnectionMultiplexer Multiplexer => null!;
        public Task<HashEntry[]> HashGetAllAsync(RedisKey key, CommandFlags flags = CommandFlags.None) => throw new NotImplementedException();
        public Task<RedisValue> HashGetAsync(RedisKey key, RedisValue hashField, CommandFlags flags = CommandFlags.None) => throw new NotImplementedException();
        public Task<bool> HashExistsAsync(RedisKey key, RedisValue hashField, CommandFlags flags = CommandFlags.None) => throw new NotImplementedException();
        public Task<long> HashLengthAsync(RedisKey key, CommandFlags flags = CommandFlags.None) => throw new NotImplementedException();
        public Task<RedisValue[]> HashGetAsync(RedisKey key, RedisValue[] hashFields, CommandFlags flags = CommandFlags.None) => throw new NotImplementedException();
        public Task<RedisValue[]> HashKeysAsync(RedisKey key, CommandFlags flags = CommandFlags.None) => throw new NotImplementedException();
        public Task<RedisValue[]> HashValuesAsync(RedisKey key, CommandFlags flags = CommandFlags.None) => throw new NotImplementedException();
        public Task<long> HashSetAsync(RedisKey key, HashEntry[] hashFields, CommandFlags flags = CommandFlags.None) => throw new NotImplementedException();
        public Task<bool> HashSetAsync(RedisKey key, RedisValue hashField, RedisValue value, When when = When.Always, CommandFlags flags = CommandFlags.None) => throw new NotImplementedException();
        public Task<long> HashDeleteAsync(RedisKey key, RedisValue[] hashFields, CommandFlags flags = CommandFlags.None) => throw new NotImplementedException();
        public Task<bool> HashDeleteAsync(RedisKey key, RedisValue hashField, CommandFlags flags = CommandFlags.None) => throw new NotImplementedException();
        public Task<long> HashIncrementAsync(RedisKey key, RedisValue hashField, long increment, CommandFlags flags = CommandFlags.None) => throw new NotImplementedException();
        public Task<double> HashIncrementAsync(RedisKey key, RedisValue hashField, double increment, CommandFlags flags = CommandFlags.None) => throw new NotImplementedException();
        public Task<long> HashStrLenAsync(RedisKey key, RedisValue hashField, CommandFlags flags = CommandFlags.None) => throw new NotImplementedException();
        public Task<long> HashRandomFieldAsync(RedisKey key, long count, CommandFlags flags = CommandFlags.None) => throw new NotImplementedException();
        public Task<HashEntry[]> HashRandomFieldWithValuesAsync(RedisKey key, long count, CommandFlags flags = CommandFlags.None) => throw new NotImplementedException();
        public Task<RedisValue> HashRandomFieldAsync(RedisKey key, CommandFlags flags = CommandFlags.None) => throw new NotImplementedException();
        public Task<long> ListLengthAsync(RedisKey key, CommandFlags flags = CommandFlags.None) => throw new NotImplementedException();
        public Task<RedisValue> ListGetByIndexAsync(RedisKey key, long index, CommandFlags flags = CommandFlags.None) => throw new NotImplementedException();
        public IAsyncEnumerable<RedisValue> ListRangeAsync(RedisKey key, long start = 0, long stop = -1, CommandFlags flags = CommandFlags.None) => throw new NotImplementedException();
        public Task<long> ListSetByIndexAsync(RedisKey key, long index, RedisValue value, CommandFlags flags = CommandFlags.None) => throw new NotImplementedException();
        public Task<long> ListInsertBeforeAsync(RedisKey key, RedisValue pivot, RedisValue value, CommandFlags flags = CommandFlags.None) => throw new NotImplementedException();
        public Task<long> ListInsertAfterAsync(RedisKey key, RedisValue pivot, RedisValue value, CommandFlags flags = CommandFlags.None) => throw new NotImplementedException();
        public Task<long> ListLeftPushAsync(RedisKey key, RedisValue value, When when = When.Always, CommandFlags flags = CommandFlags.None) => throw new NotImplementedException();
        public Task<long> ListLeftPushAsync(RedisKey key, RedisValue[] values, When when = When.Always, CommandFlags flags = CommandFlags.None) => throw new NotImplementedException();
        public Task<long> ListRightPushAsync(RedisKey key, RedisValue value, When when = When.Always, CommandFlags flags = CommandFlags.None) => throw new NotImplementedException();
        public Task<long> ListRightPushAsync(RedisKey key, RedisValue[] values, When when = When.Always, CommandFlags flags = CommandFlags.None) => throw new NotImplementedException();
        public Task<RedisValue> ListLeftPopAsync(RedisKey key, CommandFlags flags = CommandFlags.None) => throw new NotImplementedException();
        public Task<RedisValue[]> ListLeftPopAsync(RedisKey key, long count, CommandFlags flags = CommandFlags.None) => throw new NotImplementedException();
        public Task<RedisValue> ListRightPopAsync(RedisKey key, CommandFlags flags = CommandFlags.None) => throw new NotImplementedException();
        public Task<RedisValue[]> ListRightPopAsync(RedisKey key, long count, CommandFlags flags = CommandFlags.None) => throw new NotImplementedException();
        public Task<RedisValue> ListRightPopLeftPushAsync(RedisKey source, RedisKey destination, CommandFlags flags = CommandFlags.None) => throw new NotImplementedException();
        public Task<long> ListLeftTrimAsync(RedisKey key, long start, long stop, CommandFlags flags = CommandFlags.None) => throw new NotImplementedException();
        public Task<long> ListRemoveAsync(RedisKey key, RedisValue value, long count = 0, CommandFlags flags = CommandFlags.None) => throw new NotImplementedException();
        public Task SetAddAsync(RedisKey key, RedisValue value, CommandFlags flags = CommandFlags.None) => throw new NotImplementedException();
        public Task<long> SetAddAsync(RedisKey key, RedisValue[] values, CommandFlags flags = CommandFlags.None) => throw new NotImplementedException();
        public Task<bool> SetContainsAsync(RedisKey key, RedisValue value, CommandFlags flags = CommandFlags.None) => throw new NotImplementedException();
        public Task<RedisValue[]> SetMembersAsync(RedisKey key, CommandFlags flags = CommandFlags.None) => throw new NotImplementedException();
        public Task<long> SetLengthAsync(RedisKey key, CommandFlags flags = CommandFlags.None) => throw new NotImplementedException();
        public Task<long> SetRemoveAsync(RedisKey key, RedisValue value, CommandFlags flags = CommandFlags.None) => throw new NotImplementedException();
        public Task<long> SetRemoveAsync(RedisKey key, RedisValue[] values, CommandFlags flags = CommandFlags.None) => throw new NotImplementedException();
        public Task<long> SetCombineAndStoreAsync(SetOperation operation, RedisKey destination, RedisKey first, RedisKey second, CommandFlags flags = CommandFlags.None) => throw new NotImplementedException();
        public Task<long> SetCombineAndStoreAsync(SetOperation operation, RedisKey destination, RedisKey[] keys, CommandFlags flags = CommandFlags.None) => throw new NotImplementedException();
        public IAsyncEnumerable<RedisValue> SetCombineAsync(SetOperation operation, RedisKey first, RedisKey second, CommandFlags flags = CommandFlags.None) => throw new NotImplementedException();
        public IAsyncEnumerable<RedisValue> SetCombineAsync(SetOperation operation, RedisKey[] keys, CommandFlags flags = CommandFlags.None) => throw new NotImplementedException();
        public Task<RedisValue> SetRandomMemberAsync(RedisKey key, CommandFlags flags = CommandFlags.None) => throw new NotImplementedException();
        public Task<RedisValue[]> SetRandomMembersAsync(RedisKey key, long count, CommandFlags flags = CommandFlags.None) => throw new NotImplementedException();
        public Task<long> SetPopAsync(RedisKey key, long count, CommandFlags flags = CommandFlags.None) => throw new NotImplementedException();
        public Task<RedisValue> SetPopAsync(RedisKey key, CommandFlags flags = CommandFlags.None) => throw new NotImplementedException();
        public Task<bool> SetMoveAsync(RedisKey source, RedisKey destination, RedisValue value, CommandFlags flags = CommandFlags.None) => throw new NotImplementedException();
        public Task<long> SortedSetAddAsync(RedisKey key, SortedSetEntry value, CommandFlags flags = CommandFlags.None) => throw new NotImplementedException();
        public Task<long> SortedSetAddAsync(RedisKey key, SortedSetEntry[] values, CommandFlags flags = CommandFlags.None) => throw new NotImplementedException();
        public Task<bool> SortedSetContainsAsync(RedisKey key, RedisValue member, CommandFlags flags = CommandFlags.None) => throw new NotImplementedException();
        public Task<double?> SortedSetScoreAsync(RedisKey key, RedisValue member, CommandFlags flags = CommandFlags.None) => throw new NotImplementedException();
        public Task<long> SortedSetLengthAsync(RedisKey key, double? min = null, double? max = null, Exclude exclude = Exclude.None, CommandFlags flags = CommandFlags.None) => throw new NotImplementedException();
        public Task<long> SortedSetLengthByValueAsync(RedisKey key, RedisValue min, RedisValue max, Exclude exclude = Exclude.None, CommandFlags flags = CommandFlags.None) => throw new NotImplementedException();
        public Task<long> SortedSetCountByScoreAsync(RedisKey key, double min, double max, Exclude exclude = Exclude.None, CommandFlags flags = CommandFlags.None) => throw new NotImplementedException();
        public Task<long> SortedSetCountByScoreAsync(RedisKey key, RedisValue min, RedisValue max, Exclude exclude = Exclude.None, CommandFlags flags = CommandFlags.None) => throw new NotImplementedException();
        public Task<long> SortedSetRemoveAsync(RedisKey key, RedisValue member, CommandFlags flags = CommandFlags.None) => throw new NotImplementedException();
        public Task<long> SortedSetRemoveAsync(RedisKey key, RedisValue[] members, CommandFlags flags = CommandFlags.None) => throw new NotImplementedException();
        public Task<long> SortedSetRemoveRangeByScoreAsync(RedisKey key, double start, double stop, Exclude exclude = Exclude.None, CommandFlags flags = CommandFlags.None) => throw new NotImplementedException();
        public Task<long> SortedSetRemoveRangeByScoreAsync(RedisKey key, RedisValue min, RedisValue max, Exclude exclude = Exclude.None, CommandFlags flags = CommandFlags.None) => throw new NotImplementedException();
        public Task<long> SortedSetRemoveRangeByRankAsync(RedisKey key, long start, long stop, CommandFlags flags = CommandFlags.None) => throw new NotImplementedException();
        public Task<long> SortedSetRemoveRangeByLexAsync(RedisKey key, RedisValue min, RedisValue max, Exclude exclude = Exclude.None, CommandFlags flags = CommandFlags.None) => throw new NotImplementedException();
        public Task<double> SortedSetIncrementAsync(RedisKey key, RedisValue member, double increment, CommandFlags flags = CommandFlags.None) => throw new NotImplementedException();
        public Task<long> StringAppendAsync(RedisKey key, RedisValue value, CommandFlags flags = CommandFlags.None) => throw new NotImplementedException();
        public Task<long> StringBitCountAsync(RedisKey key, long start = 0, long end = -1, StringIndexType indexType = StringIndexType.Byte, CommandFlags flags = CommandFlags.None) => throw new NotImplementedException();
        public Task<long> StringBitFieldAsync(RedisKey key, in RedisValue[][] operations, CommandFlags flags = CommandFlags.None) => throw new NotImplementedException();
        public Task<long> StringBitOpAsync(Bitwise operation, RedisKey destination, RedisKey first, RedisKey second = default, CommandFlags flags = CommandFlags.None) => throw new NotImplementedException();
        public Task<long> StringBitOpAsync(Bitwise operation, RedisKey destination, RedisKey[] keys, CommandFlags flags = CommandFlags.None) => throw new NotImplementedException();
        public Task<long> StringBitPosAsync(RedisKey key, bool bit, long start = 0, long end = -1, StringIndexType indexType = StringIndexType.Byte, CommandFlags flags = CommandFlags.None) => throw new NotImplementedException();
        public Task<long> StringIncrementAsync(RedisKey key, long increment = 1, CommandFlags flags = CommandFlags.None) => throw new NotImplementedException();
        public Task<double> StringIncrementAsync(RedisKey key, double increment, CommandFlags flags = CommandFlags.None) => throw new NotImplementedException();
        public Task<RedisValue[]> StringGetAsync(RedisKey[] keys, CommandFlags flags = CommandFlags.None) => throw new NotImplementedException();
        public Task<RedisValue> StringGetDeleteAsync(RedisKey key, CommandFlags flags = CommandFlags.None) => throw new NotImplementedException();
        public Task<RedisValue> StringGetExAsync(RedisKey key, long? expirationSeconds = null, ExpireWhen expireWhen = ExpireWhen.Always, CommandFlags flags = CommandFlags.None) => throw new NotImplementedException();
        public Task<RedisValue> StringGetExAsync(RedisKey key, TimeSpan? expirationTimeSpan, ExpireWhen expireWhen = ExpireWhen.Always, CommandFlags flags = CommandFlags.None) => throw new NotImplementedException();
        public Task<RedisValue> StringGetExAsync(RedisKey key, DateTime expiration, ExpireWhen expireWhen = ExpireWhen.Always, CommandFlags flags = CommandFlags.None) => throw new NotImplementedException();
        public Task<RedisValue> StringGetRangeAsync(RedisKey key, long start, long end, CommandFlags flags = CommandFlags.None) => throw new NotImplementedException();
        public Task<bool> StringSetRangeAsync(RedisKey key, long offset, RedisValue value, CommandFlags flags = CommandFlags.None) => throw new NotImplementedException();
        public Task<long> StringLengthAsync(RedisKey key, CommandFlags flags = CommandFlags.None) => throw new NotImplementedException();
        public Task<bool> LockTakeAsync(RedisKey key, RedisValue value, TimeSpan expiry, CommandFlags flags = CommandFlags.None) => throw new NotImplementedException();
        public Task<bool> LockReleaseAsync(RedisKey key, RedisValue value, CommandFlags flags = CommandFlags.None) => throw new NotImplementedException();
        public Task<long> LockExtendAsync(RedisKey key, RedisValue value, TimeSpan expiry, CommandFlags flags = CommandFlags.None) => throw new NotImplementedException();
        public Task<long> ListMoveAsync(RedisKey source, RedisKey destination, ListSide sourceSide, ListSide destinationSide, CommandFlags flags = CommandFlags.None) => throw new NotImplementedException();
        public Task<RedisValue> ListMoveAsync(RedisKey source, RedisKey destination, ListSide sourceSide, ListSide destinationSide, long count, CommandFlags flags = CommandFlags.None) => throw new NotImplementedException();
        public Task<GeoPosition?> GeoPositionAsync(RedisKey key, RedisValue member, CommandFlags flags = CommandFlags.None) => throw new NotImplementedException();
        public Task<double?> GeoDistanceAsync(RedisKey key, RedisValue member1, RedisValue member2, GeoUnit unit = GeoUnit.Meters, CommandFlags flags = CommandFlags.None) => throw new NotImplementedException();
        public Task<HashEntry[]> GeoHashAsync(RedisKey key, RedisValue[] members, CommandFlags flags = CommandFlags.None) => throw new NotImplementedException();
        public Task<RedisValue> GeoHashAsync(RedisKey key, RedisValue member, CommandFlags flags = CommandFlags.None) => throw new NotImplementedException();
        public Task<GeoRadiusResult[]> GeoRadiusAsync(RedisKey key, GeoPosition center, double radius, GeoUnit unit = GeoUnit.Meters, int count = -1, Order? order = null, CommandFlags flags = CommandFlags.None) => throw new NotImplementedException();
        public Task<GeoRadiusResult[]> GeoRadiusByMemberAsync(RedisKey key, RedisValue member, double radius, GeoUnit unit = GeoUnit.Meters, int count = -1, Order? order = null, CommandFlags flags = CommandFlags.None) => throw new NotImplementedException();
        public Task<long> GeoAddAsync(RedisKey key, double longitude, double latitude, RedisValue member, CommandFlags flags = CommandFlags.None) => throw new NotImplementedException();
        public Task<long> GeoAddAsync(RedisKey key, GeoEntry value, CommandFlags flags = CommandFlags.None) => throw new NotImplementedException();
        public Task<long> GeoAddAsync(RedisKey key, GeoEntry[] values, CommandFlags flags = CommandFlags.None) => throw new NotImplementedException();
        public Task<long> GeoRemoveAsync(RedisKey key, RedisValue member, CommandFlags flags = CommandFlags.None) => throw new NotImplementedException();
        public Task<long> GeoRemoveAsync(RedisKey key, RedisValue[] members, CommandFlags flags = CommandFlags.None) => throw new NotImplementedException();
        public Task<long> GeoSearchAsync(RedisKey key, GeoSearchQuery query, CommandFlags flags = CommandFlags.None) => throw new NotImplementedException();
        public Task<long> GeoSearchStoreAsync(RedisKey destination, RedisKey key, GeoSearchQuery query, CommandFlags flags = CommandFlags.None) => throw new NotImplementedException();
        public Task<HyperLogLogMergeResult> HyperLogLogMergeAsync(RedisKey destination, RedisKey first, RedisKey second, CommandFlags flags = CommandFlags.None) => throw new NotImplementedException();
        public Task<HyperLogLogMergeResult> HyperLogLogMergeAsync(RedisKey destination, RedisKey[] keys, CommandFlags flags = CommandFlags.None) => throw new NotImplementedException();
        public Task<HyperLogLogCountResult> HyperLogLogCountAsync(RedisKey key, CommandFlags flags = CommandFlags.None) => throw new NotImplementedException();
        public Task<HyperLogLogCountResult> HyperLogLogCountAsync(RedisKey[] keys, CommandFlags flags = CommandFlags.None) => throw new NotImplementedException();
        public Task<bool> HyperLogLogAddAsync(RedisKey key, RedisValue value, CommandFlags flags = CommandFlags.None) => throw new NotImplementedException();
        public Task<bool> HyperLogLogAddAsync(RedisKey key, RedisValue[] values, CommandFlags flags = CommandFlags.None) => throw new NotImplementedException();
        public IAsyncEnumerable<RedisKey> KeysAsync(int database = -1, RedisValue pattern = default, int pageSize = 10, CommandFlags flags = CommandFlags.None) => throw new NotImplementedException();
        public Task<RedisValue> DebugObjectAsync(RedisKey key, CommandFlags flags = CommandFlags.None) => throw new NotImplementedException();
        public Task KeyDeleteAsync(RedisKey key, CommandFlags flags = CommandFlags.None) => throw new NotImplementedException();
        public Task<long> KeyDeleteAsync(RedisKey[] keys, CommandFlags flags = CommandFlags.None) => throw new NotImplementedException();
        public Task<RedisValue> KeyDumpAsync(RedisKey key, CommandFlags flags = CommandFlags.None) => throw new NotImplementedException();
        public Task<bool> KeyExistsAsync(RedisKey key, CommandFlags flags = CommandFlags.None) => throw new NotImplementedException();
        public Task<long> KeyExistsAsync(RedisKey[] keys, CommandFlags flags = CommandFlags.None) => throw new NotImplementedException();
        public Task<bool> KeyExpireAsync(RedisKey key, long expirationSeconds, CommandFlags flags = CommandFlags.None) => throw new NotImplementedException();
        public Task<bool> KeyExpireAsync(RedisKey key, TimeSpan expiration, CommandFlags flags = CommandFlags.None) => throw new NotImplementedException();
        public Task<bool> KeyExpireAsync(RedisKey key, DateTime expiration, CommandFlags flags = CommandFlags.None) => throw new NotImplementedException();
        public Task<bool> KeyExpireAsync(RedisKey key, long? expirationSeconds, ExpireWhen expireWhen = ExpireWhen.Always, CommandFlags flags = CommandFlags.None) => throw new NotImplementedException();
        public Task<bool> KeyExpireAsync(RedisKey key, TimeSpan? expiration, ExpireWhen expireWhen = ExpireWhen.Always, CommandFlags flags = CommandFlags.None) => throw new NotImplementedException();
        public Task<bool> KeyExpireAsync(RedisKey key, DateTime expiration, ExpireWhen expireWhen = ExpireWhen.Always, CommandFlags flags = CommandFlags.None) => throw new NotImplementedException();
        public Task<long> KeyExpireTimeAsync(RedisKey key, CommandFlags flags = CommandFlags.None) => throw new NotImplementedException();
        public Task<long> KeyPExpireTimeAsync(RedisKey key, CommandFlags flags = CommandFlags.None) => throw new NotImplementedException();
        public Task<long> KeyIdleTimeAsync(RedisKey key, CommandFlags flags = CommandFlags.None) => throw new NotImplementedException();
        public Task<bool> KeyPersistAsync(RedisKey key, CommandFlags flags = CommandFlags.None) => throw new NotImplementedException();
        public Task<bool> KeyPExpireAsync(RedisKey key, long expiration, CommandFlags flags = CommandFlags.None) => throw new NotImplementedException();
        public Task<bool> KeyPExpireAsync(RedisKey key, TimeSpan expiration, CommandFlags flags = CommandFlags.None) => throw new NotImplementedException();
        public Task<long> KeyRandomAsync(CommandFlags flags = CommandFlags.None) => throw new NotImplementedException();
        public Task<bool> KeyRenameAsync(RedisKey key, RedisKey newKey, When when = When.Always, CommandFlags flags = CommandFlags.None) => throw new NotImplementedException();
        public Task<bool> KeyRestoreAsync(RedisKey key, RedisValue value, TimeSpan ttl, CommandFlags flags = CommandFlags.None) => throw new NotImplementedException();
        public Task<long?> KeyTimeToLiveAsync(RedisKey key, CommandFlags flags = CommandFlags.None) => throw new NotImplementedException();
        public Task<long?> KeyPTimeToLiveAsync(RedisKey key, CommandFlags flags = CommandFlags.None) => throw new NotImplementedException();
        public Task<RedisType> KeyTypeAsync(RedisKey key, CommandFlags flags = CommandFlags.None) => throw new NotImplementedException();
        public Task<long> KeyScanAsync(RedisKey key, RedisValue pattern = default, int pageSize = 10, long cursor = 0, int count = -1, CommandFlags flags = CommandFlags.None) => throw new NotImplementedException();
        public Task<RedisValue> ScriptEvaluateAsync(string script, RedisKey[] keys = null, RedisValue[] values = null, CommandFlags flags = CommandFlags.None) => throw new NotImplementedException();
        public Task<RedisValue> ScriptEvaluateAsync(byte[] hash, RedisKey[] keys = null, RedisValue[] values = null, CommandFlags flags = CommandFlags.None) => throw new NotImplementedException();
        public Task<RedisValue> ScriptEvaluateAsync(LuaScript script, object parameters = null, CommandFlags flags = CommandFlags.None) => throw new NotImplementedException();
        public Task<RedisValue> ScriptEvaluateAsync(LoadedLuaScript script, object parameters = null, CommandFlags flags = CommandFlags.None) => throw new NotImplementedException();
        public Task<long> StreamAcknowledgeAsync(RedisKey key, RedisValue group, RedisValue messageId, CommandFlags flags = CommandFlags.None) => throw new NotImplementedException();
        public Task<long> StreamAcknowledgeAsync(RedisKey key, RedisValue group, RedisValue[] messageIds, CommandFlags flags = CommandFlags.None) => throw new NotImplementedException();
        public Task<RedisValue> StreamAddAsync(RedisKey key, RedisValue streamField, RedisValue streamValue, RedisValue? maxLength = null, bool? useApproximateMaxLength = null, CommandFlags flags = CommandFlags.None) => throw new NotImplementedException();
        public Task<RedisValue> StreamAddAsync(RedisKey key, NameValueEntry[] nameValueEntries, RedisValue? maxLength = null, bool? useApproximateMaxLength = null, CommandFlags flags = CommandFlags.None) => throw new NotImplementedException();
        public Task<StreamEntry[]> StreamClaimAsync(RedisKey key, RedisValue group, RedisValue consumer, long minIdleTimeInMs, RedisValue[] messageIds, CommandFlags flags = CommandFlags.None) => throw new NotImplementedException();
        public Task<bool> StreamConsumerGroupSetIdAsync(RedisKey key, RedisValue group, RedisValue id, CommandFlags flags = CommandFlags.None) => throw new NotImplementedException();
        public Task<RedisValue> StreamConsumerGroupCreateAsync(RedisKey key, RedisValue group, RedisValue? id = null, CommandFlags flags = CommandFlags.None) => throw new NotImplementedException();
        public IAsyncEnumerable<StreamConsumerInfo> StreamConsumerInfoAsync(RedisKey key, RedisValue group, CommandFlags flags = CommandFlags.None) => throw new NotImplementedException();
        public Task<bool> StreamConsumerDeleteAsync(RedisKey key, RedisValue group, RedisValue consumer, CommandFlags flags = CommandFlags.None) => throw new NotImplementedException();
        public Task<long> StreamDeleteAsync(RedisKey key, RedisValue[] messageIds, CommandFlags flags = CommandFlags.None) => throw new NotImplementedException();
        public Task<long> StreamDeleteConsumerGroupAsync(RedisKey key, RedisValue group, CommandFlags flags = CommandFlags.None) => throw new NotImplementedException();
        public IAsyncEnumerable<StreamGroupInfo> StreamGroupInfoAsync(RedisKey key, CommandFlags flags = CommandFlags.None) => throw new NotImplementedException();
        public IAsyncEnumerable<StreamEntry> StreamReadAsync(StreamPosition streamPosition, CommandFlags flags = CommandFlags.None) => throw new NotImplementedException();
        public IAsyncEnumerable<StreamEntry> StreamReadAsync(StreamPosition streamPosition, int? count = null, bool countIsLowerBound = false, CommandFlags flags = CommandFlags.None) => throw new NotImplementedException();
        public IAsyncEnumerable<StreamEntry> StreamReadAsync(StreamPosition[] streamPositions, CommandFlags flags = CommandFlags.None) => throw new NotImplementedException();
        public IAsyncEnumerable<StreamEntry> StreamReadAsync(StreamPosition[] streamPositions, int? count = null, bool countIsLowerBound = false, CommandFlags flags = CommandFlags.None) => throw new NotImplementedException();
        public IAsyncEnumerable<StreamEntry> StreamReadGroupAsync(RedisValue group, RedisValue consumer, StreamPosition[] streamPositions, int? count = null, bool countIsLowerBound = false, CommandFlags flags = CommandFlags.None) => throw new NotImplementedException();
        public IAsyncEnumerable<StreamEntry> StreamReadGroupAsync(RedisKey[] keys, RedisValue group, RedisValue consumer, int? count = null, CommandFlags flags = CommandFlags.None) => throw new NotImplementedException();
        public Task<long> StreamLengthAsync(RedisKey key, CommandFlags flags = CommandFlags.None) => throw new NotImplementedException();
        public Task<StreamInfo> StreamInfoAsync(RedisKey key, CommandFlags flags = CommandFlags.None) => throw new NotImplementedException();
        public Task<long> StreamGroupDestroyAsync(RedisKey key, RedisValue group, CommandFlags flags = CommandFlags.None) => throw new NotImplementedException();
        public Task<long> StreamTrimAsync(RedisKey key, RedisValue? minId = null, bool? useApproximateMinId = null, long? maxLen = null, bool? useApproximateMaxLen = null, CommandFlags flags = CommandFlags.None) => throw new NotImplementedException();
        public Task<long> StreamPendingCountAsync(RedisKey key, RedisValue group, CommandFlags flags = CommandFlags.None) => throw new NotImplementedException();
        public Task<StreamPendingMessageInfo[]> StreamPendingMessagesAsync(RedisKey key, RedisValue group, int count, RedisValue consumerNameFilter, CommandFlags flags = CommandFlags.None) => throw new NotImplementedException();
    }

    [Fact]
    public async Task AcquireLock_ReturnsToken_When_StringSetSucceeds()
    {
        var stubDb = new StubDatabase(setAsyncReturnValue: true);
        var redisLock = new RedisLock(stubDb);
        var token = await redisLock.AcquireLockAsync("test-key", TimeSpan.FromSeconds(5));

        Assert.NotNull(token);
        Assert.NotEmpty(token);
    }

    [Fact]
    public async Task AcquireLock_ReturnsNull_When_StringSetFails()
    {
        var stubDb = new StubDatabase(setAsyncReturnValue: false);
        var redisLock = new RedisLock(stubDb);
        var token = await redisLock.AcquireLockAsync("test-key", TimeSpan.FromSeconds(5));

        Assert.Null(token);
    }

    [Fact]
    public async Task ReleaseLock_ReturnsTrue_When_ValueMatchesAndDeleteSucceeds()
    {
        var stubDb = new StubDatabase(getAsyncReturnValue: (RedisValue)"token123", keyDeleteAsyncReturnValue: true);
        var redisLock = new RedisLock(stubDb);
        var result = await redisLock.ReleaseLockAsync("test-key", "token123");

        Assert.True(result);
    }

    [Fact]
    public async Task ReleaseLock_ReturnsFalse_When_ValueDoesNotMatch()
    {
        var stubDb = new StubDatabase(getAsyncReturnValue: (RedisValue)"other-token");
        var redisLock = new RedisLock(stubDb);
        var result = await redisLock.ReleaseLockAsync("test-key", "token123");

        Assert.False(result);
    }
}
