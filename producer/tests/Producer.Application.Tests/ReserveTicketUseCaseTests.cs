using FluentAssertions;
using NSubstitute;
using Producer.Application.Dtos;
using Producer.Application.Ports.Outbound;
using Producer.Application.UseCases;
using Producer.Domain.Events;
using Xunit;

namespace Producer.Application.Tests;

public class ReserveTicketUseCaseTests
{
    private readonly ITicketEventPublisher _ticketPublisher;
    private readonly ReserveTicketUseCase _sut;

    public ReserveTicketUseCaseTests()
    {
        _ticketPublisher = Substitute.For<ITicketEventPublisher>();
        _sut = new ReserveTicketUseCase(_ticketPublisher);
    }

    [Fact]
    public async Task ExecuteAsync_WithValidRequest_ShouldPublishEvent()
    {
        var request = new ReserveTicketRequest
        {
            EventId = 1,
            TicketId = 100,
            OrderId = "ORDER-001",
            ReservedBy = "user@example.com",
            ExpiresInSeconds = 300
        };

        _ticketPublisher.PublishAsync(Arg.Any<TicketReservedEvent>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        var result = await _sut.ExecuteAsync(request);

        result.Success.Should().BeTrue();
        result.TicketId.Should().Be(100);
        result.Message.Should().Be("Reserva procesada");
        await _ticketPublisher.Received(1).PublishAsync(Arg.Any<TicketReservedEvent>(), Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData(0, "TicketId debe ser mayor a 0")]
    [InlineData(-1, "TicketId debe ser mayor a 0")]
    public async Task ExecuteAsync_WithInvalidTicketId_ShouldReturnError(long ticketId, string expectedMessage)
    {
        var request = new ReserveTicketRequest
        {
            EventId = 1,
            TicketId = ticketId,
            OrderId = "ORDER-001",
            ReservedBy = "user@example.com"
        };

        var result = await _sut.ExecuteAsync(request);

        result.Success.Should().BeFalse();
        result.Message.Should().Be(expectedMessage);
        await _ticketPublisher.DidNotReceive().PublishAsync(Arg.Any<TicketReservedEvent>(), Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData(0, "EventId debe ser mayor a 0")]
    [InlineData(-5, "EventId debe ser mayor a 0")]
    public async Task ExecuteAsync_WithInvalidEventId_ShouldReturnError(long eventId, string expectedMessage)
    {
        var request = new ReserveTicketRequest
        {
            EventId = eventId,
            TicketId = 100,
            OrderId = "ORDER-001",
            ReservedBy = "user@example.com"
        };

        var result = await _sut.ExecuteAsync(request);

        result.Success.Should().BeFalse();
        result.Message.Should().Be(expectedMessage);
    }

    [Fact]
    public async Task ExecuteAsync_WithNullOrderId_ShouldReturnError()
    {
        var request = new ReserveTicketRequest
        {
            EventId = 1,
            TicketId = 100,
            OrderId = null,
            ReservedBy = "user@example.com"
        };

        var result = await _sut.ExecuteAsync(request);

        result.Success.Should().BeFalse();
        result.Message.Should().Be("OrderId es requerido");
    }

    [Fact]
    public async Task ExecuteAsync_WithNullReservedBy_ShouldReturnError()
    {
        var request = new ReserveTicketRequest
        {
            EventId = 1,
            TicketId = 100,
            OrderId = "ORDER-001",
            ReservedBy = null
        };

        var result = await _sut.ExecuteAsync(request);

        result.Success.Should().BeFalse();
        result.Message.Should().Be("ReservedBy es requerido");
    }
}
