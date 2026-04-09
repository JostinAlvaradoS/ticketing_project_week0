using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Waitlist.Application.UseCases.AssignNext;
using Waitlist.Domain.Entities;
using Waitlist.Infrastructure.Persistence;
using Waitlist.Infrastructure.Workers;

namespace Waitlist.IntegrationTests;

public class WaitlistIntegrationTests : IClassFixture<WaitlistWebApplicationFactory>, IAsyncLifetime
{
    private readonly WaitlistWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public WaitlistIntegrationTests(WaitlistWebApplicationFactory factory)
    {
        _factory = factory;
        _client  = factory.CreateClient();
    }

    public async Task InitializeAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<WaitlistDbContext>();
        var entries = db.WaitlistEntries.ToList();
        db.WaitlistEntries.RemoveRange(entries);
        await db.SaveChangesAsync();

        _factory.CatalogMock.Reset();
        _factory.OrderingMock.Reset();
        _factory.InventoryMock.Reset();
        _factory.EmailMock.Reset();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task TI01_Join_StockZero_Returns201AndPersistsEntry()
    {
        var eventId = Guid.NewGuid();
        _factory.CatalogMock
            .Setup(c => c.GetAvailableCountAsync(eventId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        var response = await _client.PostAsJsonAsync("/api/v1/waitlist/join", new
        {
            email   = "user@test.com",
            eventId
        });

        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("entryId").GetGuid().Should().NotBeEmpty();
        body.GetProperty("position").GetInt32().Should().Be(1);

        using var scope = _factory.Services.CreateScope();
        var db    = scope.ServiceProvider.GetRequiredService<WaitlistDbContext>();
        var entry = db.WaitlistEntries.Single(e => e.EventId == eventId);
        entry.Status.Should().Be(WaitlistEntry.StatusPending);
        entry.Email.Should().Be("user@test.com");
    }

    [Fact]
    public async Task TI02_Join_StockAvailable_Returns409WithMessage()
    {
        var eventId = Guid.NewGuid();
        _factory.CatalogMock
            .Setup(c => c.GetAvailableCountAsync(eventId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(5);

        var response = await _client.PostAsJsonAsync("/api/v1/waitlist/join", new
        {
            email   = "user@test.com",
            eventId
        });

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("message").GetString().Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task TI03_Join_DuplicateEmail_Returns409WithListaDeEsperaMessage()
    {
        var eventId = Guid.NewGuid();
        _factory.CatalogMock
            .Setup(c => c.GetAvailableCountAsync(eventId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<WaitlistDbContext>();
            db.WaitlistEntries.Add(WaitlistEntry.Create("user@test.com", eventId));
            await db.SaveChangesAsync();
        }

        var response = await _client.PostAsJsonAsync("/api/v1/waitlist/join", new
        {
            email   = "user@test.com",
            eventId
        });

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("message").GetString()!
            .ToLowerInvariant().Should().Contain("lista de espera");
    }

    [Fact]
    public async Task TI04_AssignNext_PendingEntryExists_AssignsEntryAndCreatesOrder()
    {
        var eventId = Guid.NewGuid();
        var seatId  = Guid.NewGuid();
        var orderId = Guid.NewGuid();

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<WaitlistDbContext>();
            db.WaitlistEntries.Add(WaitlistEntry.Create("user@test.com", eventId));
            await db.SaveChangesAsync();
        }

        _factory.OrderingMock
            .Setup(o => o.CreateWaitlistOrderAsync(seatId, It.IsAny<decimal>(), "user@test.com", eventId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(orderId);
        _factory.EmailMock
            .Setup(e => e.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(true);

        using var commandScope = _factory.Services.CreateScope();
        var mediator = commandScope.ServiceProvider.GetRequiredService<IMediator>();
        await mediator.Send(new AssignNextCommand(seatId, eventId));

        using var verifyScope = _factory.Services.CreateScope();
        var db2   = verifyScope.ServiceProvider.GetRequiredService<WaitlistDbContext>();
        var entry = db2.WaitlistEntries.Single(e => e.EventId == eventId);
        entry.Status.Should().Be(WaitlistEntry.StatusAssigned);
        entry.SeatId.Should().Be(seatId);
        entry.OrderId.Should().Be(orderId);

        _factory.OrderingMock.Verify(o =>
            o.CreateWaitlistOrderAsync(seatId, It.IsAny<decimal>(), "user@test.com", eventId, It.IsAny<CancellationToken>()),
            Times.Once);
        _factory.EmailMock.Verify(e =>
            e.SendAsync("user@test.com", It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()),
            Times.Once);
    }

    [Fact]
    public async Task TI05_AssignNext_EmptyQueue_NoOrderCreatedNoInventoryReleased()
    {
        var seatId  = Guid.NewGuid();
        var eventId = Guid.NewGuid();

        using var scope  = _factory.Services.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        await mediator.Send(new AssignNextCommand(seatId, eventId));

        _factory.OrderingMock.Verify(o =>
            o.CreateWaitlistOrderAsync(It.IsAny<Guid>(), It.IsAny<decimal>(), It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _factory.InventoryMock.Verify(i =>
            i.ReleaseSeatAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task TI06_ExpiryWorker_WithNextInQueue_RotatesWithoutReleasingToInventory()
    {
        var eventId    = Guid.NewGuid();
        var seatId     = Guid.NewGuid();
        var oldOrderId = Guid.NewGuid();
        var newOrderId = Guid.NewGuid();

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<WaitlistDbContext>();

            var expiredEntry = WaitlistEntry.Create("expiring@test.com", eventId);
            expiredEntry.Assign(seatId, oldOrderId);
            SetExpiresAt(expiredEntry, DateTime.UtcNow.AddMinutes(-5));
            db.WaitlistEntries.Add(expiredEntry);

            db.WaitlistEntries.Add(WaitlistEntry.Create("next@test.com", eventId));

            await db.SaveChangesAsync();
        }

        _factory.OrderingMock
            .Setup(o => o.CancelOrderAsync(oldOrderId, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _factory.OrderingMock
            .Setup(o => o.CreateWaitlistOrderAsync(seatId, It.IsAny<decimal>(), "next@test.com", eventId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(newOrderId);
        _factory.EmailMock
            .Setup(e => e.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(true);

        var scopeFactory = _factory.Services.GetRequiredService<IServiceScopeFactory>();
        var worker = new WaitlistExpiryWorker(scopeFactory);
        await worker.ProcessExpiredEntriesAsync(CancellationToken.None);

        using var verifyScope = _factory.Services.CreateScope();
        var db2     = verifyScope.ServiceProvider.GetRequiredService<WaitlistDbContext>();
        var entries = db2.WaitlistEntries.Where(e => e.EventId == eventId).ToList();

        entries.Single(e => e.Email == "expiring@test.com").Status.Should().Be(WaitlistEntry.StatusExpired);
        entries.Single(e => e.Email == "next@test.com").Status.Should().Be(WaitlistEntry.StatusAssigned);

        _factory.OrderingMock.Verify(o =>
            o.CancelOrderAsync(oldOrderId, It.IsAny<CancellationToken>()),
            Times.Once);
        _factory.OrderingMock.Verify(o =>
            o.CreateWaitlistOrderAsync(seatId, It.IsAny<decimal>(), "next@test.com", eventId, It.IsAny<CancellationToken>()),
            Times.Once);
        _factory.InventoryMock.Verify(i =>
            i.ReleaseSeatAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task TI07_ExpiryWorker_EmptyQueue_ReleasesSeatToInventoryAndCancelsOrder()
    {
        var eventId = Guid.NewGuid();
        var seatId  = Guid.NewGuid();
        var orderId = Guid.NewGuid();

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<WaitlistDbContext>();

            var expiredEntry = WaitlistEntry.Create("expiring@test.com", eventId);
            expiredEntry.Assign(seatId, orderId);
            SetExpiresAt(expiredEntry, DateTime.UtcNow.AddMinutes(-5));
            db.WaitlistEntries.Add(expiredEntry);

            await db.SaveChangesAsync();
        }

        _factory.InventoryMock
            .Setup(i => i.ReleaseSeatAsync(seatId, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _factory.OrderingMock
            .Setup(o => o.CancelOrderAsync(orderId, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _factory.EmailMock
            .Setup(e => e.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(true);

        var scopeFactory = _factory.Services.GetRequiredService<IServiceScopeFactory>();
        var worker = new WaitlistExpiryWorker(scopeFactory);
        await worker.ProcessExpiredEntriesAsync(CancellationToken.None);

        using var verifyScope = _factory.Services.CreateScope();
        var db2 = verifyScope.ServiceProvider.GetRequiredService<WaitlistDbContext>();
        db2.WaitlistEntries.Single(e => e.EventId == eventId)
            .Status.Should().Be(WaitlistEntry.StatusExpired);

        _factory.InventoryMock.Verify(i =>
            i.ReleaseSeatAsync(seatId, It.IsAny<CancellationToken>()),
            Times.Once);
        _factory.OrderingMock.Verify(o =>
            o.CancelOrderAsync(orderId, It.IsAny<CancellationToken>()),
            Times.Once);
        _factory.OrderingMock.Verify(o =>
            o.CreateWaitlistOrderAsync(It.IsAny<Guid>(), It.IsAny<decimal>(), It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    private static void SetExpiresAt(WaitlistEntry entry, DateTime value) =>
        typeof(WaitlistEntry)
            .GetProperty(nameof(WaitlistEntry.ExpiresAt))!
            .SetValue(entry, value);
}
