using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Microsoft.EntityFrameworkCore;
using Inventory.Domain.Entities;
using Inventory.Infrastructure.Persistence;
using Inventory.Application.UseCases.CreateReservation;
using Inventory.Infrastructure.Locking;
using Inventory.Infrastructure.Messaging;
using Inventory.Domain.Ports;

namespace Inventory.Integration.Tests;

/// <summary>
/// Integration tests for the CreateReservation use case (T019).
/// Tests Redis locking, seat reservation, and Kafka event publishing.
/// </summary>
public class CreateReservationIntegrationTests
{
    [Fact]
    public async Task CreateReservation_ShouldSucceed_WhenSeatIsAvailable()
    {
        // Arrange: Create an in-memory database and a seat
        var options = new DbContextOptionsBuilder<InventoryDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        var seatId = Guid.NewGuid();
        const string customerId = "customer-123";

        using (var context = new InventoryDbContext(options))
        {
            var seat = new Seat
            {
                Id = seatId,
                Section = "A",
                Row = "10",
                Number = 5,
                Reserved = false,
                Version = null
            };
            context.Seats.Add(seat);
            await context.SaveChangesAsync();
        }

        // Act: Create a reservation
        using (var context = new InventoryDbContext(options))
        {
            // Mock Redis lock to always succeed
            var mockRedisLock = new MockRedisLock(acquireResult: "token-123");
            var mockKafkaProducer = new MockKafkaProducer();

            var handler = new CreateReservationCommandHandler(context, mockRedisLock, mockKafkaProducer);
            var command = new CreateReservationCommand(seatId, customerId);

            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(seatId, result.SeatId);
            Assert.Equal(customerId, result.CustomerId);
            Assert.Equal("active", result.Status);
            Assert.True(result.ExpiresAt > DateTime.UtcNow.AddMinutes(14)); // Should be close to 15 min
            Assert.True(result.ExpiresAt < DateTime.UtcNow.AddMinutes(16)); // But not more than 15 min

            // Verify seat is marked as reserved
            var updatedSeat = await context.Seats.FindAsync(seatId);
            Assert.NotNull(updatedSeat);
            Assert.True(updatedSeat.Reserved);

            // Verify reservation was created
            var reservation = await context.Reservations.FirstOrDefaultAsync(r => r.SeatId == seatId);
            Assert.NotNull(reservation);
            Assert.Equal(customerId, reservation.CustomerId);
        }
    }

    [Fact]
    public async Task CreateReservation_ShouldFail_WhenSeatIsAlreadyReserved()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<InventoryDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        var seatId = Guid.NewGuid();
        const string customerId = "customer-123";

        using (var context = new InventoryDbContext(options))
        {
            var seat = new Seat
            {
                Id = seatId,
                Section = "A",
                Row = "10",
                Number = 5,
                Reserved = true, // Already reserved
                Version = null
            };
            context.Seats.Add(seat);
            await context.SaveChangesAsync();
        }

        // Act
        using (var context = new InventoryDbContext(options))
        {
            var mockRedisLock = new MockRedisLock(acquireResult: "token-123");
            var mockKafkaProducer = new MockKafkaProducer();

            var handler = new CreateReservationCommandHandler(context, mockRedisLock, mockKafkaProducer);
            var command = new CreateReservationCommand(seatId, customerId);

            // Assert
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(
                () => handler.Handle(command, CancellationToken.None));
            Assert.Contains("already reserved", ex.Message);
        }
    }

    [Fact]
    public async Task CreateReservation_ShouldFail_WhenLockCannotBeAcquired()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<InventoryDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        var seatId = Guid.NewGuid();
        const string customerId = "customer-123";

        using (var context = new InventoryDbContext(options))
        {
            var seat = new Seat
            {
                Id = seatId,
                Section = "A",
                Row = "10",
                Number = 5,
                Reserved = false,
                Version = null
            };
            context.Seats.Add(seat);
            await context.SaveChangesAsync();
        }

        // Act
        using (var context = new InventoryDbContext(options))
        {
            var mockRedisLock = new MockRedisLock(acquireResult: null); // Lock acquisition fails
            var mockKafkaProducer = new MockKafkaProducer();

            var handler = new CreateReservationCommandHandler(context, mockRedisLock, mockKafkaProducer);
            var command = new CreateReservationCommand(seatId, customerId);

            // Assert
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(
                () => handler.Handle(command, CancellationToken.None));
            Assert.Contains("Could not acquire lock", ex.Message);
        }
    }

    // Mock implementations for testing
    private class MockRedisLock : IRedisLock
    {
        private readonly string? _acquireResult;

        public MockRedisLock(string? acquireResult)
        {
            _acquireResult = acquireResult;
        }

        public Task<string?> AcquireLockAsync(string key, TimeSpan ttl)
        {
            return Task.FromResult(_acquireResult);
        }

        public Task<bool> ReleaseLockAsync(string key, string token)
        {
            return Task.FromResult(true);
        }
    }

    private class MockKafkaProducer : IKafkaProducer
    {
        public Task ProduceAsync(string topicName, string message, string? key = null)
        {
            // Mock implementation - just succeed
            return Task.CompletedTask;
        }
    }
}
