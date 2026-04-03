using FluentAssertions;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Waitlist.Application.UseCases.AssignNext;
using Waitlist.Infrastructure.Consumers;

namespace Waitlist.UnitTests.Application;

public class ReservationExpiredConsumerTests
{
    private readonly Mock<IMediator> _mediatorMock = new();
    private readonly Mock<IServiceScopeFactory> _scopeFactoryMock;
    private readonly ReservationExpiredConsumer _consumer;

    public ReservationExpiredConsumerTests()
    {
        var serviceProviderMock = new Mock<IServiceProvider>();
        serviceProviderMock.Setup(x => x.GetService(typeof(IMediator))).Returns(_mediatorMock.Object);

        var scopeMock = new Mock<IServiceScope>();
        scopeMock.Setup(x => x.ServiceProvider).Returns(serviceProviderMock.Object);

        _scopeFactoryMock = new Mock<IServiceScopeFactory>();
        _scopeFactoryMock.Setup(x => x.CreateScope()).Returns(scopeMock.Object);

        var loggerMock = new Mock<Microsoft.Extensions.Logging.ILogger<ReservationExpiredConsumer>>();
        _consumer = new ReservationExpiredConsumer(_scopeFactoryMock.Object, loggerMock.Object, "localhost:9092");
    }

    // Cycle 15 — Valid v3 payload dispatches AssignNextCommand
    [Fact]
    public async Task ProcessMessage_ValidV3Payload_DispatchesAssignNextCommand()
    {
        var concertEventId = Guid.NewGuid();
        var seatId = Guid.NewGuid();
        var json = $"{{\"messageId\":\"msg-1\",\"concertEventId\":\"{concertEventId:D}\",\"seatId\":\"{seatId:D}\"}}";

        await _consumer.ProcessMessageAsync(json, default);

        _mediatorMock.Verify(m => m.Send(
            It.Is<AssignNextCommand>(c => c.EventId == concertEventId && c.SeatId == seatId),
            default), Times.Once);
    }

    // Cycle 15 — Missing concertEventId → does not dispatch
    [Fact]
    public async Task ProcessMessage_MissingConcertEventId_DoesNotDispatch()
    {
        var json = $"{{\"messageId\":\"msg-1\",\"seatId\":\"{Guid.NewGuid():D}\"}}";

        await _consumer.ProcessMessageAsync(json, default);

        _mediatorMock.Verify(m => m.Send(It.IsAny<AssignNextCommand>(), default), Times.Never);
    }
}
