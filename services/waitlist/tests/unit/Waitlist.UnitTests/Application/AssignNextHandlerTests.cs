using FluentAssertions;
using Moq;
using Waitlist.Application.Ports;
using Waitlist.Application.UseCases.AssignNext;
using Waitlist.Domain.Entities;

namespace Waitlist.UnitTests.Application;

public class AssignNextHandlerTests
{
    private readonly Mock<IWaitlistRepository> _repoMock = new();
    private readonly Mock<IOrderingClient> _orderingMock = new();
    private readonly Mock<IEmailService> _emailMock = new();
    private readonly AssignNextHandler _handler;

    public AssignNextHandlerTests()
    {
        _handler = new AssignNextHandler(_repoMock.Object, _orderingMock.Object, _emailMock.Object);
    }

    // Cycle 12 — Happy path: pending entry exists
    [Fact]
    public async Task Handle_PendingEntryExists_AssignsEntryAndSendsEmail()
    {
        var eventId = Guid.NewGuid();
        var seatId = Guid.NewGuid();
        var orderId = Guid.NewGuid();
        var entry = WaitlistEntry.Create("user@example.com", eventId);

        _repoMock.Setup(r => r.HasAssignedEntryForSeatAsync(seatId, default)).ReturnsAsync(false);
        _repoMock.Setup(r => r.GetNextPendingAsync(eventId, default)).ReturnsAsync(entry);
        _orderingMock.Setup(o => o.CreateWaitlistOrderAsync("user@example.com", seatId, eventId, default)).ReturnsAsync(orderId);
        _repoMock.Setup(r => r.UpdateAsync(entry, default)).Returns(Task.CompletedTask);
        _emailMock.Setup(e => e.SendWaitlistAssignmentAsync("user@example.com", seatId, It.IsAny<DateTime>(), default))
            .Returns(Task.CompletedTask);

        await _handler.Handle(new AssignNextCommand(eventId, seatId), default);

        entry.Status.Should().Be(WaitlistEntry.StatusAssigned);
        entry.SeatId.Should().Be(seatId);
        _repoMock.Verify(r => r.UpdateAsync(entry, default), Times.Once);
        _emailMock.Verify(e => e.SendWaitlistAssignmentAsync("user@example.com", seatId, It.IsAny<DateTime>(), default), Times.Once);
    }

    // Cycle 13 — Empty queue → no action
    [Fact]
    public async Task Handle_EmptyQueue_NoOrderCreatedAndNoUpdate()
    {
        var eventId = Guid.NewGuid();
        var seatId = Guid.NewGuid();

        _repoMock.Setup(r => r.HasAssignedEntryForSeatAsync(seatId, default)).ReturnsAsync(false);
        _repoMock.Setup(r => r.GetNextPendingAsync(eventId, default)).ReturnsAsync((WaitlistEntry?)null);

        await _handler.Handle(new AssignNextCommand(eventId, seatId), default);

        _orderingMock.Verify(o => o.CreateWaitlistOrderAsync(It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<Guid>(), default), Times.Never);
        _repoMock.Verify(r => r.UpdateAsync(It.IsAny<WaitlistEntry>(), default), Times.Never);
    }

    // Cycle 14 — Idempotency: seat already assigned
    [Fact]
    public async Task Handle_SeatAlreadyAssigned_SkipsProcessing()
    {
        var eventId = Guid.NewGuid();
        var seatId = Guid.NewGuid();

        _repoMock.Setup(r => r.HasAssignedEntryForSeatAsync(seatId, default)).ReturnsAsync(true);

        await _handler.Handle(new AssignNextCommand(eventId, seatId), default);

        _repoMock.Verify(r => r.GetNextPendingAsync(It.IsAny<Guid>(), default), Times.Never);
        _orderingMock.Verify(o => o.CreateWaitlistOrderAsync(It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<Guid>(), default), Times.Never);
    }
}
