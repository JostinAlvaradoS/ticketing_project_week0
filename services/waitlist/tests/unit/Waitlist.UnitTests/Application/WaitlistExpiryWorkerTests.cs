using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Waitlist.Application.Ports;
using Waitlist.Domain.Entities;
using Waitlist.Infrastructure.Workers;

namespace Waitlist.UnitTests.Application;

public class WaitlistExpiryWorkerTests
{
    private readonly Mock<IWaitlistRepository> _repoMock = new();
    private readonly Mock<IOrderingClient> _orderingMock = new();
    private readonly Mock<IInventoryClient> _inventoryMock = new();
    private readonly Mock<IEmailService> _emailMock = new();
    private readonly Mock<IServiceScopeFactory> _scopeFactoryMock;
    private readonly WaitlistExpiryWorker _worker;

    public WaitlistExpiryWorkerTests()
    {
        var serviceProviderMock = new Mock<IServiceProvider>();
        serviceProviderMock.Setup(x => x.GetService(typeof(IWaitlistRepository))).Returns(_repoMock.Object);
        serviceProviderMock.Setup(x => x.GetService(typeof(IOrderingClient))).Returns(_orderingMock.Object);
        serviceProviderMock.Setup(x => x.GetService(typeof(IInventoryClient))).Returns(_inventoryMock.Object);
        serviceProviderMock.Setup(x => x.GetService(typeof(IEmailService))).Returns(_emailMock.Object);

        var scopeMock = new Mock<IServiceScope>();
        scopeMock.Setup(x => x.ServiceProvider).Returns(serviceProviderMock.Object);

        _scopeFactoryMock = new Mock<IServiceScopeFactory>();
        _scopeFactoryMock.Setup(x => x.CreateScope()).Returns(scopeMock.Object);

        _worker = new WaitlistExpiryWorker(_scopeFactoryMock.Object);
    }

    private WaitlistEntry CreateExpiredEntry()
    {
        var entry = WaitlistEntry.Create("user@example.com", Guid.NewGuid());
        entry.Assign(Guid.NewGuid(), Guid.NewGuid());
        // Force ExpiresAt to past
        typeof(WaitlistEntry).GetProperty(nameof(WaitlistEntry.ExpiresAt))!
            .SetValue(entry, DateTime.UtcNow.AddHours(-1));
        return entry;
    }

    // Cycle 17 — Rotation: expired + next pending
    [Fact]
    public async Task ProcessExpired_WithNextPending_ExpiresCurrentAndAssignsNext()
    {
        var expired = CreateExpiredEntry();
        var next = WaitlistEntry.Create("next@example.com", expired.EventId);
        var newOrderId = Guid.NewGuid();

        _repoMock.Setup(r => r.GetExpiredAssignedAsync(default)).ReturnsAsync(new[] { expired });
        _repoMock.Setup(r => r.GetNextPendingAsync(expired.EventId, default)).ReturnsAsync(next);
        _orderingMock.Setup(o => o.CancelOrderAsync(expired.OrderId!.Value, default)).Returns(Task.CompletedTask);
        _orderingMock.Setup(o => o.CreateWaitlistOrderAsync("next@example.com", expired.SeatId!.Value, expired.EventId, default)).ReturnsAsync(newOrderId);
        _repoMock.Setup(r => r.UpdateAsync(It.IsAny<WaitlistEntry>(), default)).Returns(Task.CompletedTask);
        _emailMock.Setup(e => e.SendWaitlistAssignmentAsync("next@example.com", expired.SeatId!.Value, It.IsAny<DateTime>(), default)).Returns(Task.CompletedTask);

        await _worker.ProcessExpiredEntriesAsync();

        expired.Status.Should().Be(WaitlistEntry.StatusExpired);
        next.Status.Should().Be(WaitlistEntry.StatusAssigned);
        _emailMock.Verify(e => e.SendWaitlistAssignmentAsync("next@example.com", expired.SeatId!.Value, It.IsAny<DateTime>(), default), Times.Once);
    }

    // Cycle 18 — Empty queue → release seat
    [Fact]
    public async Task ProcessExpired_EmptyQueue_ReleasesSeatAndCancelsOrder()
    {
        var expired = CreateExpiredEntry();

        _repoMock.Setup(r => r.GetExpiredAssignedAsync(default)).ReturnsAsync(new[] { expired });
        _repoMock.Setup(r => r.GetNextPendingAsync(expired.EventId, default)).ReturnsAsync((WaitlistEntry?)null);
        _orderingMock.Setup(o => o.CancelOrderAsync(expired.OrderId!.Value, default)).Returns(Task.CompletedTask);
        _inventoryMock.Setup(i => i.ReleaseSeatAsync(expired.SeatId!.Value, default)).Returns(Task.CompletedTask);
        _repoMock.Setup(r => r.UpdateAsync(It.IsAny<WaitlistEntry>(), default)).Returns(Task.CompletedTask);

        await _worker.ProcessExpiredEntriesAsync();

        expired.Status.Should().Be(WaitlistEntry.StatusExpired);
        _inventoryMock.Verify(i => i.ReleaseSeatAsync(expired.SeatId!.Value, default), Times.Once);
    }

    // Cycle 19 — No expired entries → no actions
    [Fact]
    public async Task ProcessExpired_NoExpiredEntries_NoActionsPerformed()
    {
        _repoMock.Setup(r => r.GetExpiredAssignedAsync(default))
            .ReturnsAsync(Array.Empty<WaitlistEntry>());

        await _worker.ProcessExpiredEntriesAsync();

        _repoMock.Verify(r => r.UpdateAsync(It.IsAny<WaitlistEntry>(), default), Times.Never);
        _inventoryMock.Verify(i => i.ReleaseSeatAsync(It.IsAny<Guid>(), default), Times.Never);
    }
}
