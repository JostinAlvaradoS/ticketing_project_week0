using System.Text.Json;
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
/// TDD Ciclo 20 — RED: verifica que el payload de reservation-expired v3
/// usa messageId (no eventId) y expone concertEventId.
/// </summary>
public class ReservationExpiryWorkerTests
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

    [Fact]
    public async Task ProcessExpiredReservations_PublishesPayload_WithMessageIdAndConcertEventId()
    {
        // Arrange
        var dbName = $"inventory_test_{Guid.NewGuid():N}";
        await using var db = CreateInMemoryDb(dbName);

        var concertEventId = Guid.NewGuid();
        var seatId = Guid.NewGuid();

        var seat = new Seat { Id = seatId, Section = "A", Row = "1", Number = 5 };
        db.Seats.Add(seat);

        // Reservation.Create must accept eventId — this will FAIL until T052 adds EventId to domain
        var reservation = Reservation.Create(seatId, "customer@example.com", concertEventId, ttlMinutes: 1);
        // Force expiry by setting ExpiresAt in the past via reflection
        typeof(Reservation)
            .GetProperty(nameof(Reservation.ExpiresAt))!
            .SetValue(reservation, DateTime.UtcNow.AddMinutes(-5));

        db.Reservations.Add(reservation);
        await db.SaveChangesAsync();

        string? capturedJson = null;
        var producerMock = new Mock<IKafkaProducer>();
        producerMock
            .Setup(p => p.ProduceAsync("reservation-expired", It.IsAny<string>(), It.IsAny<string?>()))
            .Callback<string, string, string?>((_, json, _) => capturedJson = json)
            .Returns(Task.CompletedTask);

        var scopeFactory = CreateScopeFactory(db);
        var worker = new ReservationExpiryWorker(scopeFactory, producerMock.Object, waitlistClient: null, TimeSpan.FromHours(1));

        // Act
        await worker.ProcessExpiredReservationsAsync(CancellationToken.None);

        // Assert — payload must use messageId (not eventId) and include concertEventId
        capturedJson.Should().NotBeNull();

        using var doc = JsonDocument.Parse(capturedJson!);
        var root = doc.RootElement;

        root.TryGetProperty("messageId", out _).Should().BeTrue("payload v3 must have messageId");
        root.TryGetProperty("eventId", out _).Should().BeFalse("legacy eventId must be renamed to messageId");
        root.TryGetProperty("concertEventId", out var concertEventProp).Should().BeTrue("payload v3 must have concertEventId");
        concertEventProp.GetString().Should().Be(concertEventId.ToString("D"));
    }
}
