using System;
using System.Threading.Tasks;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using StackExchange.Redis;
using Xunit;

namespace Inventory.Infrastructure.Tests;

public class RedisLockIntegrationTests
{
    [Fact]
    public async Task AcquireAndRelease_Lock_With_RealRedisContainer()
    {
        var redisContainer = new TestcontainersBuilder<TestcontainersContainer>()
            .WithImage("redis:7.0")
            .WithCleanUp(true)
            .WithName($"test-redis-{Guid.NewGuid():N}")
            .WithPortBinding(6379, true)
            .Build();

        await redisContainer.StartAsync();
        try
        {
            var host = redisContainer.Hostname;
            var port = redisContainer.GetMappedPublicPort(6379);
            var connStr = $"{host}:{port}";

            using var mux = await ConnectionMultiplexer.ConnectAsync(connStr);
            var db = mux.GetDatabase();

            var key = "integration-lock-key";
            var token = Guid.NewGuid().ToString("N");

            var set = await db.StringSetAsync(key, token, TimeSpan.FromSeconds(5), when: When.NotExists);
            Assert.True(set, "Expected first set to succeed");

            // Second attempt should fail
            var token2 = Guid.NewGuid().ToString("N");
            var set2 = await db.StringSetAsync(key, token2, TimeSpan.FromSeconds(5), when: When.NotExists);
            Assert.False(set2, "Expected second set to fail while lock held");

            // Wait for expiry
            await Task.Delay(TimeSpan.FromSeconds(6));

            var set3 = await db.StringSetAsync(key, token2, TimeSpan.FromSeconds(5), when: When.NotExists);
            Assert.True(set3, "Expected set to succeed after TTL expiry");

            // Clean up
            await db.KeyDeleteAsync(key);
        }
        finally
        {
            await redisContainer.StopAsync();
        }
    }
}
