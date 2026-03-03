using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Xunit;
using Inventory.Infrastructure.Persistence;
using Inventory.Domain.Entities;
using Inventory.Infrastructure.Workers;
using Inventory.Domain.Ports;
using Inventory.Application.UseCases.CreateReservation;
using Microsoft.Extensions.DependencyInjection;

namespace Inventory.Integration.Tests;

public class ReservationAndRaceTests
{
    [Fact]
    public async Task ExpiryWorker_ProcessExpiredReservations_MarksExpiredAndPublishesEvent()
    {
        var options = new DbContextOptionsBuilder<InventoryDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        var seatId = Guid.NewGuid();
        var reservationId = Guid.NewGuid();

        using (var ctx = new InventoryDbContext(options))
        {
            ctx.Seats.Add(new Seat { Id = seatId, Section = "A", Row = "1", Number = 1, Reserved = true });
            ctx.Reservations.Add(new Reservation
            {
                Id = reservationId,
                SeatId = seatId,
                CustomerId = "c1",
                CreatedAt = DateTime.UtcNow.AddMinutes(-20),
                ExpiresAt = DateTime.UtcNow.AddMinutes(-5),
                Status = "active"
            });
            await ctx.SaveChangesAsync();
        }

        using (var ctx = new InventoryDbContext(options))
        {
            var scopeFactory = new TestScopeFactory(ctx);
            var mockProducer = new TestKafkaProducer();
            var worker = new ReservationExpiryWorker(scopeFactory, mockProducer, TimeSpan.FromSeconds(1));

            await worker.ProcessExpiredReservationsAsync(CancellationToken.None);

            var res = await ctx.Reservations.FindAsync(reservationId);
            Assert.NotNull(res);
            Assert.Equal("expired", res.Status);

            var seat = await ctx.Seats.FindAsync(seatId);
            Assert.NotNull(seat);
            Assert.False(seat.Reserved);

            Assert.True(mockProducer.Published);
            Assert.Contains(reservationId.ToString("D"), mockProducer.LastMessage);
        }
    }

    [Fact]
    public async Task TwoClients_RacingToReserve_OnlyOneReservationCreated()
    {
        var options = new DbContextOptionsBuilder<InventoryDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        var seatId = Guid.NewGuid();

        using (var ctx = new InventoryDbContext(options))
        {
            var seat = new Seat { Id = seatId, Section = "A", Row = "1", Number = 1, Reserved = false };
            ctx.Seats.Add(seat);
            await ctx.SaveChangesAsync();
        }

        // Shared in-memory DB across contexts
        var lockAdapter = new SimpleConcurrentRedisLock();
        var producer = new TestKafkaProducer();

        var tasks = Enumerable.Range(0, 2).Select(async i =>
        {
            using var ctx = new InventoryDbContext(options);
            var handler = new CreateReservationCommandHandler(ctx, lockAdapter, producer);
            try
            {
                var res = await handler.Handle(new CreateReservationCommand(seatId, $"customer-{i}"), CancellationToken.None);
                return (success: true, reservationId: res.ReservationId);
            }
            catch
            {
                return (success: false, reservationId: Guid.Empty);
            }
        }).ToArray();

        var results = await Task.WhenAll(tasks);

        // Only one should succeed
        var successCount = results.Count(r => r.success);
        Assert.Equal(1, successCount);

        using (var ctx = new InventoryDbContext(options))
        {
            var reservations = await ctx.Reservations.Where(r => r.SeatId == seatId).ToListAsync();
            Assert.Single(reservations);
            var seat = await ctx.Seats.FindAsync(seatId);
            Assert.True(seat!.Reserved);
        }
    }

    // Test helpers
    private class TestScopeFactory : IServiceScopeFactory
    {
        private readonly InventoryDbContext _ctx;
        public TestScopeFactory(InventoryDbContext ctx) => _ctx = ctx;
        public IServiceScope CreateScope() => new TestScope(_ctx);

        private class TestScope : IServiceScope
        {
            public TestScope(InventoryDbContext ctx)
            {
                ServiceProvider = new TestServiceProvider(ctx);
            }
            public IServiceProvider ServiceProvider { get; }
            public void Dispose() { }
        }

        private class TestServiceProvider : IServiceProvider
        {
            private readonly InventoryDbContext _ctx;
            public TestServiceProvider(InventoryDbContext ctx) => _ctx = ctx;
            public object? GetService(Type serviceType)
            {
                if (serviceType == typeof(InventoryDbContext)) return _ctx;
                return null;
            }
        }
    }

    private class TestKafkaProducer : IKafkaProducer
    {
        public bool Published { get; private set; }
        public string? LastMessage { get; private set; }
        public Task ProduceAsync(string topicName, string message, string? key = null)
        {
            Published = true;
            LastMessage = message;
            return Task.CompletedTask;
        }
    }

    private class SimpleConcurrentRedisLock : Inventory.Domain.Ports.IRedisLock
    {
        private readonly System.Collections.Concurrent.ConcurrentDictionary<string, string> _locks = new();

        public Task<string?> AcquireLockAsync(string key, TimeSpan ttl)
        {
            var token = Guid.NewGuid().ToString("N");
            var added = _locks.TryAdd(key, token);
            return Task.FromResult(added ? token : null);
        }

        public Task<bool> ReleaseLockAsync(string key, string token)
        {
            _locks.TryRemove(key, out _);
            return Task.FromResult(true);
        }
    }
}
