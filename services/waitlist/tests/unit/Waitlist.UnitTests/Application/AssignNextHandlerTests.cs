// TDD Ciclos 12-14 + 16 — Spec US2: Asignación Automática
// STATUS: 🔴 RED — AssignNextHandler, CompleteAssignmentHandler do not exist yet

using FluentAssertions;
using Moq;
using Waitlist.Application.Ports;
using Waitlist.Application.UseCases.AssignNext;
using Waitlist.Application.UseCases.CompleteAssignment;
using Waitlist.Domain.Entities;

namespace Waitlist.UnitTests.Application;

public class AssignNextHandlerTests
{
    private readonly Mock<IWaitlistRepository> _repoMock;
    private readonly Mock<IOrderingClient>     _orderingMock;
    private readonly Mock<IEmailService>       _emailMock;
    private readonly AssignNextHandler         _handler;

    public AssignNextHandlerTests()
    {
        _repoMock     = new Mock<IWaitlistRepository>();
        _orderingMock = new Mock<IOrderingClient>();
        _emailMock    = new Mock<IEmailService>();
        _handler      = new AssignNextHandler(_repoMock.Object, _orderingMock.Object, _emailMock.Object);
    }

    // ─────────────────────────────────────────────────────────────
    // Ciclo 12 — Spec US2 Scenario 1
    // Given hay pending → crea orden, asigna, notifica
    // ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_PendingEntryExists_AssignsEntryAndSendsEmail()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var seatId  = Guid.NewGuid();
        var orderId = Guid.NewGuid();
        var entry   = WaitlistEntry.Create("jostin@example.com", eventId);

        var command = new AssignNextCommand(seatId, eventId);

        _repoMock
            .Setup(x => x.HasAssignedEntryForSeatAsync(seatId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _repoMock
            .Setup(x => x.GetNextPendingAsync(eventId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(entry);

        _orderingMock
            .Setup(x => x.CreateWaitlistOrderAsync(seatId, It.IsAny<decimal>(), entry.Email, eventId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(orderId);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        entry.Status.Should().Be(WaitlistEntry.StatusAssigned);
        entry.SeatId.Should().Be(seatId);
        entry.OrderId.Should().Be(orderId);

        _repoMock.Verify(x => x.UpdateAsync(entry, It.IsAny<CancellationToken>()), Times.Once);
        _emailMock.Verify(x => x.SendAsync(entry.Email, It.IsAny<string>(), It.IsAny<string>(), null), Times.Once);
    }

    // ─────────────────────────────────────────────────────────────
    // Ciclo 13 — Spec US2 Scenario 2
    // Given cola vacía → no acción
    // ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_EmptyQueue_NoOrderCreatedAndNoUpdate()
    {
        // Arrange
        var seatId  = Guid.NewGuid();
        var eventId = Guid.NewGuid();
        var command = new AssignNextCommand(seatId, eventId);

        _repoMock
            .Setup(x => x.HasAssignedEntryForSeatAsync(seatId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _repoMock
            .Setup(x => x.GetNextPendingAsync(eventId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((WaitlistEntry?)null);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _orderingMock.Verify(x => x.CreateWaitlistOrderAsync(
            It.IsAny<Guid>(), It.IsAny<decimal>(), It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Never);

        _repoMock.Verify(x => x.UpdateAsync(It.IsAny<WaitlistEntry>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    // ─────────────────────────────────────────────────────────────
    // Ciclo 14 — Edge case: idempotencia (reservation-expired duplicado)
    // Given seatId ya tiene entrada Assigned → skip
    // ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_SeatAlreadyAssigned_SkipsProcessing()
    {
        // Arrange
        var seatId  = Guid.NewGuid();
        var eventId = Guid.NewGuid();
        var command = new AssignNextCommand(seatId, eventId);

        _repoMock
            .Setup(x => x.HasAssignedEntryForSeatAsync(seatId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert — ninguna consulta a la cola ni creación de orden
        _repoMock.Verify(x => x.GetNextPendingAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
        _orderingMock.Verify(x => x.CreateWaitlistOrderAsync(
            It.IsAny<Guid>(), It.IsAny<decimal>(), It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }
}

public class CompleteAssignmentHandlerTests
{
    private readonly Mock<IWaitlistRepository> _repoMock;
    private readonly CompleteAssignmentHandler _handler;

    public CompleteAssignmentHandlerTests()
    {
        _repoMock = new Mock<IWaitlistRepository>();
        _handler  = new CompleteAssignmentHandler(_repoMock.Object);
    }

    // ─────────────────────────────────────────────────────────────
    // Ciclo 16 — Spec US2 Scenario 3
    // Given payment-succeeded para una orden de waitlist → Completed
    // ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_AssignedEntry_SetsStatusCompleted()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var entry   = WaitlistEntry.Create("jostin@example.com", Guid.NewGuid());
        entry.Assign(Guid.NewGuid(), orderId);

        _repoMock
            .Setup(x => x.GetByOrderIdAsync(orderId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(entry);

        var command = new CompleteAssignmentCommand(orderId);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        entry.Status.Should().Be(WaitlistEntry.StatusCompleted);
        _repoMock.Verify(x => x.UpdateAsync(entry, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_NullEntry_DoesNothing()
    {
        // Arrange — orderId no pertenece a ninguna entrada de waitlist
        var orderId = Guid.NewGuid();
        _repoMock
            .Setup(x => x.GetByOrderIdAsync(orderId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((WaitlistEntry?)null);

        var command = new CompleteAssignmentCommand(orderId);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert — no persiste nada
        _repoMock.Verify(x => x.UpdateAsync(It.IsAny<WaitlistEntry>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
