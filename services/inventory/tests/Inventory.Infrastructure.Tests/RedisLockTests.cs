using StackExchange.Redis;
using Inventory.Infrastructure.Redis;
using Moq;
using Xunit;
using System.Collections.Generic;
using System.Threading.Tasks;
using System;

namespace Inventory.Infrastructure.Tests;

public class RedisLockTests
{
    [Fact]
    public async Task AcquireLock_ReturnsToken_When_StringSetSucceeds()
    {
        var mockDb = new Mock<IDatabase>();
        mockDb.Setup(db => db.StringSetAsync(It.IsAny<RedisKey>(), It.IsAny<RedisValue>(), It.IsAny<TimeSpan?>(), false, It.IsAny<When>(), It.IsAny<CommandFlags>()))
              .ReturnsAsync(true);

        var redisLock = new RedisLock(mockDb.Object);
        var token = await redisLock.AcquireLockAsync("test-key", TimeSpan.FromSeconds(5));

        Assert.NotNull(token);
        Assert.NotEmpty(token);
    }

    [Fact]
    public async Task AcquireLock_ReturnsNull_When_StringSetFails()
    {
        var mockDb = new Mock<IDatabase>();
        mockDb.Setup(db => db.StringSetAsync(It.IsAny<RedisKey>(), It.IsAny<RedisValue>(), It.IsAny<TimeSpan?>(), false, It.IsAny<When>(), It.IsAny<CommandFlags>()))
              .ReturnsAsync(false);

        var redisLock = new RedisLock(mockDb.Object);
        var token = await redisLock.AcquireLockAsync("test-key", TimeSpan.FromSeconds(5));

        Assert.Null(token);
    }

    [Fact]
    public async Task ReleaseLock_ReturnsTrue_When_ValueMatchesAndDeleteSucceeds()
    {
        var mockDb = new Mock<IDatabase>();
        mockDb.Setup(db => db.StringGetAsync(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>()))
              .ReturnsAsync((RedisValue)"token123");
        mockDb.Setup(db => db.KeyDeleteAsync(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>()))
              .ReturnsAsync(true);

        var redisLock = new RedisLock(mockDb.Object);
        var result = await redisLock.ReleaseLockAsync("test-key", "token123");

        Assert.True(result);
    }

    [Fact]
    public async Task ReleaseLock_ReturnsFalse_When_ValueDoesNotMatch()
    {
        var mockDb = new Mock<IDatabase>();
        mockDb.Setup(db => db.StringGetAsync(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>()))
              .ReturnsAsync((RedisValue)"other-token");

        var redisLock = new RedisLock(mockDb.Object);
        var result = await redisLock.ReleaseLockAsync("test-key", "token123");

        Assert.False(result);
    }
}
