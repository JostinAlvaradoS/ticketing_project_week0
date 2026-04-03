using FluentAssertions;
using Inventory.Application.Ports;
using Inventory.Domain.Entities;
using Inventory.Infrastructure.Persistence;
using Inventory.Infrastructure.Workers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace Inventory.UnitTests.Infrastructure;

/// <summary>
/// TDD Ciclo 21 — RED: verifica que el worker consulte la lista de espera antes de liberar el asiento (ADR-03).
/// </summary>
public class ReservationExpiryWorkerWaitlistTests
{
    private static InventoryDbContext CreateInMemoryDb(string dbName)
    {
        var options = new DbContextOptionsBuilder<InventoryDbContext>()
            .UseInMemoryDatabase(databaseName: dbName)
            .Options;
        return new InventoryDbContext(options);
    }

    private static IServiceScopeFactory CreateScopeFactory(InventoryDbContext db)
    {
        var scopeMock = new Mock<IServiceScope>();
        var providerMock = new Mock<IServiceProvider>();
        var factoryMock = new Mock<IServiceScopeFactory>();

        providerMock.Setup(p => p.GetService(typeof(InventoryDbContext))).Returns(db);
        scopeMock.Setup(s => s.ServiceProvider).Returns(providerMock.Object);
        factoryMock.Setup(f => f.CreateScope()).Returns(scopeMock.Object);

        return factoryMock.Object;
    }

    private static async Task<(InventoryDbContext db, Seat seat, Reservation reservation)> SetupExpiredReservationAsync(
        string dbName, Guid eventId)
    {
        var db = CreateInMemoryDb(dbName);
        var seatId = Guid.NewGuid();

        var seat = new Seat { Id = seatId, Section = "A", Row = "1", Number = 5 };
        seat.Reserve(); // seat is currently reserved (has an active reservation)
        db.Seats.Add(seat);

        var reservation = Reservation.Create(seatId, "customer@example.com", eventId, ttlMinutes: 1);
        typeof(Reservation)
            .GetProperty(nameof(Reservation.ExpiresAt))!
            .SetValue(reservation, DateTime.UtcNow.AddMinutes(-5));

        db.Reservations.Add(reservation);
        await db.SaveChangesAsync();

        return (db, seat, reservation);
    }

    [Fact]
    public async Task ProcessExpiredReservations_WhenQueueActive_DoesNotReleaseSeat()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var dbName = $"inventory_waitlist_active_{Guid.NewGuid():N}";
        var (db, seat, _) = await SetupExpiredReservationAsync(dbName, eventId);

        var producerMock = new Mock<IKafkaProducer>();
        producerMock
            .Setup(p => p.ProduceAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>()))
            .Returns(Task.CompletedTask);

        var waitlistClientMock = new Mock<IWaitlistClient>();
        waitlistClientMock
            .Setup(w => w.HasPendingAsync(eventId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true); // queue is active

        var scopeFactory = CreateScopeFactory(db);
        // Worker must accept IWaitlistClient — this will FAIL until T055 updates the constructor
        var worker = new ReservationExpiryWorker(scopeFactory, producerMock.Object, waitlistClientMock.Object, TimeSpan.FromHours(1));

        // Act
        await worker.ProcessExpiredReservationsAsync(CancellationToken.None);

        // Assert — seat must NOT be released when queue is active
        var updatedSeat = await db.Seats.FindAsync(seat.Id);
        updatedSeat!.Reserved.Should().BeTrue("seat was reserved and should NOT be released while queue is active");
    }

    [Fact]
    public async Task ProcessExpiredReservations_WhenWaitlistClientThrows_FallbackReleasesSeat()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var dbName = $"inventory_waitlist_fallback_{Guid.NewGuid():N}";
        var (db, seat, _) = await SetupExpiredReservationAsync(dbName, eventId);

        var producerMock = new Mock<IKafkaProducer>();
        producerMock
            .Setup(p => p.ProduceAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>()))
            .Returns(Task.CompletedTask);

        var waitlistClientMock = new Mock<IWaitlistClient>();
        waitlistClientMock
            .Setup(w => w.HasPendingAsync(eventId, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("Waitlist service unavailable"));

        var scopeFactory = CreateScopeFactory(db);
        var worker = new ReservationExpiryWorker(scopeFactory, producerMock.Object, waitlistClientMock.Object, TimeSpan.FromHours(1));

        // Act
        await worker.ProcessExpiredReservationsAsync(CancellationToken.None);

        // Assert — fallback: seat IS released when waitlist check fails
        var updatedSeat = await db.Seats.FindAsync(seat.Id);
        updatedSeat!.Reserved.Should().BeFalse("fallback behavior must release the seat when waitlist is unreachable");
    }
}
