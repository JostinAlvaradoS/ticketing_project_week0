// TDD Ciclo 15 — Consumer Kafka: deserialización v3 + dispatch
// STATUS: 🔴 RED — ReservationExpiredConsumer does not exist yet

using FluentAssertions;
using MediatR;
using Moq;
using Waitlist.Application.UseCases.AssignNext;
using Waitlist.Infrastructure.Consumers;

namespace Waitlist.UnitTests.Application;

public class ReservationExpiredConsumerTests
{
    private readonly Mock<IMediator> _mediatorMock;

    public ReservationExpiredConsumerTests()
    {
        _mediatorMock = new Mock<IMediator>();
    }

    // ─────────────────────────────────────────────────────────────
    // Ciclo 15 — Deserialización payload v3 + dispatch AssignNextCommand
    // ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task ProcessMessage_ValidV3Payload_DispatchesAssignNextCommand()
    {
        // Arrange
        var seatId          = Guid.NewGuid();
        var concertEventId  = Guid.NewGuid();
        var messageId       = Guid.NewGuid();
        var reservationId   = Guid.NewGuid();

        var json = $$"""
        {
            "messageId":      "{{messageId}}",
            "reservationId":  "{{reservationId}}",
            "seatId":         "{{seatId}}",
            "customerId":     "user-123",
            "concertEventId": "{{concertEventId}}"
        }
        """;

        AssignNextCommand? capturedCommand = null;
        _mediatorMock
            .Setup(x => x.Send(It.IsAny<AssignNextCommand>(), It.IsAny<CancellationToken>()))
            .Callback<AssignNextCommand, CancellationToken>((cmd, _) => capturedCommand = cmd)
            .Returns(Task.FromResult(Unit.Value));

        // Act
        await ReservationExpiredConsumer.ProcessMessageAsync(json, _mediatorMock.Object, CancellationToken.None);

        // Assert
        capturedCommand.Should().NotBeNull();
        capturedCommand!.SeatId.Should().Be(seatId);
        capturedCommand.ConcertEventId.Should().Be(concertEventId);
    }

    [Fact]
    public async Task ProcessMessage_MissingConcertEventId_DoesNotDispatch()
    {
        // Arrange — payload v2 sin concertEventId (campo ausente → Guid.Empty o null)
        var json = """
        {
            "messageId":    "00000000-0000-0000-0000-000000000001",
            "reservationId":"00000000-0000-0000-0000-000000000002",
            "seatId":       "00000000-0000-0000-0000-000000000003",
            "customerId":   "user-123"
        }
        """;

        // Act
        await ReservationExpiredConsumer.ProcessMessageAsync(json, _mediatorMock.Object, CancellationToken.None);

        // Assert — no se despacha comando sin concertEventId válido
        _mediatorMock.Verify(x => x.Send(It.IsAny<AssignNextCommand>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
