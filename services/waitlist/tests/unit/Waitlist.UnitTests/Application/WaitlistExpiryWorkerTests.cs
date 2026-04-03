// TDD Ciclos 17-19 — Spec US3: Rotación por Inacción
// STATUS: 🔴 RED — WaitlistExpiryWorker does not exist yet

using FluentAssertions;
using Moq;
using Waitlist.Application.Ports;
using Waitlist.Domain.Entities;
using Waitlist.Infrastructure.Workers;

namespace Waitlist.UnitTests.Application;

public class WaitlistExpiryWorkerTests
{
    private readonly Mock<IWaitlistRepository> _repoMock;
    private readonly Mock<IOrderingClient>     _orderingMock;
    private readonly Mock<IInventoryClient>    _inventoryMock;
    private readonly Mock<IEmailService>       _emailMock;
    private readonly WaitlistExpiryWorker      _worker;

    public WaitlistExpiryWorkerTests()
    {
        _repoMock      = new Mock<IWaitlistRepository>();
        _orderingMock  = new Mock<IOrderingClient>();
        _inventoryMock = new Mock<IInventoryClient>();
        _emailMock     = new Mock<IEmailService>();
        _worker        = new WaitlistExpiryWorker(
            _repoMock.Object, _orderingMock.Object, _inventoryMock.Object, _emailMock.Object);
    }

    // ─────────────────────────────────────────────────────────────
    // Ciclo 17 — Spec US3 Scenario 1
    // Turno expirado + siguiente en cola → rotación
    // ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task ProcessExpired_WithNextPending_ExpiresCurrentAndAssignsNext()
    {
        // Arrange
        var eventId    = Guid.NewGuid();
        var seatId     = Guid.NewGuid();
        var expiredOrderId = Guid.NewGuid();
        var newOrderId     = Guid.NewGuid();

        // Entry expirada (Assigned, ExpiresAt en el pasado)
        var expiredEntry = WaitlistEntry.Create("expired@example.com", eventId);
        expiredEntry.Assign(seatId, expiredOrderId);
        ForceExpiresAt(expiredEntry, DateTime.UtcNow.AddMinutes(-1));

        // Siguiente en cola
        var nextEntry = WaitlistEntry.Create("next@example.com", eventId);

        _repoMock
            .Setup(x => x.GetExpiredAssignedAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<WaitlistEntry> { expiredEntry });

        _repoMock
            .Setup(x => x.GetNextPendingAsync(eventId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(nextEntry);

        _orderingMock
            .Setup(x => x.CreateWaitlistOrderAsync(seatId, It.IsAny<decimal>(), nextEntry.Email, eventId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(newOrderId);

        // Act
        await _worker.ProcessExpiredEntriesAsync(CancellationToken.None);

        // Assert
        expiredEntry.Status.Should().Be(WaitlistEntry.StatusExpired);
        nextEntry.Status.Should().Be(WaitlistEntry.StatusAssigned);
        nextEntry.SeatId.Should().Be(seatId);
        nextEntry.OrderId.Should().Be(newOrderId);

        _repoMock.Verify(x => x.UpdateAsync(expiredEntry, It.IsAny<CancellationToken>()), Times.Once);
        _repoMock.Verify(x => x.UpdateAsync(nextEntry, It.IsAny<CancellationToken>()), Times.Once);
        _emailMock.Verify(x => x.SendAsync(expiredEntry.Email, It.IsAny<string>(), It.IsAny<string>(), null), Times.Once);
        _emailMock.Verify(x => x.SendAsync(nextEntry.Email, It.IsAny<string>(), It.IsAny<string>(), null), Times.Once);
        _inventoryMock.Verify(x => x.ReleaseSeatAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    // ─────────────────────────────────────────────────────────────
    // Ciclo 18 — Spec US3 Scenario 2
    // Turno expirado + cola vacía → libera asiento al pool
    // ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task ProcessExpired_EmptyQueue_ReleasesSeatAndCancelsOrder()
    {
        // Arrange
        var eventId    = Guid.NewGuid();
        var seatId     = Guid.NewGuid();
        var orderId    = Guid.NewGuid();

        var expiredEntry = WaitlistEntry.Create("expired@example.com", eventId);
        expiredEntry.Assign(seatId, orderId);
        ForceExpiresAt(expiredEntry, DateTime.UtcNow.AddMinutes(-1));

        _repoMock
            .Setup(x => x.GetExpiredAssignedAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<WaitlistEntry> { expiredEntry });

        _repoMock
            .Setup(x => x.GetNextPendingAsync(eventId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((WaitlistEntry?)null);

        // Act
        await _worker.ProcessExpiredEntriesAsync(CancellationToken.None);

        // Assert
        expiredEntry.Status.Should().Be(WaitlistEntry.StatusExpired);
        _inventoryMock.Verify(x => x.ReleaseSeatAsync(seatId, It.IsAny<CancellationToken>()), Times.Once);
        _orderingMock.Verify(x => x.CancelOrderAsync(orderId, It.IsAny<CancellationToken>()), Times.Once);
    }

    // ─────────────────────────────────────────────────────────────
    // Ciclo 19 — Spec US3 Scenario 3
    // Entry Completed → no aparece en query → no procesada
    // ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task ProcessExpired_NoExpiredEntries_NoActionsPerformed()
    {
        // Arrange — query devuelve lista vacía (Completed entries no pasan el filtro)
        _repoMock
            .Setup(x => x.GetExpiredAssignedAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<WaitlistEntry>());

        // Act
        await _worker.ProcessExpiredEntriesAsync(CancellationToken.None);

        // Assert
        _repoMock.Verify(x => x.UpdateAsync(It.IsAny<WaitlistEntry>(), It.IsAny<CancellationToken>()), Times.Never);
        _inventoryMock.Verify(x => x.ReleaseSeatAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
        _orderingMock.Verify(x => x.CancelOrderAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    // ─────────────────────────────────────────────────────────────
    // Helpers
    // ─────────────────────────────────────────────────────────────

    private static void ForceExpiresAt(WaitlistEntry entry, DateTime expiresAt)
    {
        typeof(WaitlistEntry)
            .GetProperty(nameof(WaitlistEntry.ExpiresAt))!
            .SetValue(entry, expiresAt);
    }
}
