using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using ReservationService.Application.Dtos;
using ReservationService.Application.Ports.Outbound;
using ReservationService.Application.UseCases;
using ReservationService.Domain.Entities;
using ReservationService.Domain.Enums;
using Xunit;

namespace ReservationService.Application.Tests.UseCases;

public class ReserveTicketUseCaseTests
{
    private readonly ITicketRepository _ticketRepository;
    private readonly ILogger<ReserveTicketUseCase> _logger;
    private readonly ReserveTicketUseCase _useCase;

    public ReserveTicketUseCaseTests()
    {
        _ticketRepository = Substitute.For<ITicketRepository>();
        _logger = Substitute.For<ILogger<ReserveTicketUseCase>>();
        _useCase = new ReserveTicketUseCase(_ticketRepository, _logger);
    }

    private static ReservationMessageDto CreateMessage(long ticketId = 1) => new()
    {
        TicketId = ticketId,
        EventId = 100,
        OrderId = "order-123",
        ReservedBy = "user-456",
        ReservationDurationSeconds = 300,
        PublishedAt = DateTime.UtcNow
    };

    [Fact]
    public async Task ExecuteAsync_TicketNotFound_ReturnsFailure()
    {
        var message = CreateMessage(ticketId: 99);
        _ticketRepository.GetByIdAsync(99, Arg.Any<CancellationToken>())
            .Returns((Ticket?)null);

        var result = await _useCase.ExecuteAsync(message);

        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("not found");
    }

    [Theory]
    [InlineData(TicketStatus.Reserved)]
    [InlineData(TicketStatus.Paid)]
    [InlineData(TicketStatus.Released)]
    [InlineData(TicketStatus.Cancelled)]
    public async Task ExecuteAsync_TicketAlreadyReserved_ReturnsFailure(TicketStatus status)
    {
        var message = CreateMessage();
        var ticket = new Ticket { Id = 1, Status = status };
        _ticketRepository.GetByIdAsync(1, Arg.Any<CancellationToken>())
            .Returns(ticket);

        var result = await _useCase.ExecuteAsync(message);

        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("not available");
    }

    [Fact]
    public async Task ExecuteAsync_AvailableTicket_ReturnsSuccess()
    {
        var message = CreateMessage();
        var ticket = new Ticket { Id = 1, Status = TicketStatus.Available, Version = 1 };
        _ticketRepository.GetByIdAsync(1, Arg.Any<CancellationToken>())
            .Returns(ticket);
        _ticketRepository.TryReserveAsync(ticket, "user-456", "order-123", Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns(true);

        var result = await _useCase.ExecuteAsync(message);

        result.Success.Should().BeTrue();
        result.ErrorMessage.Should().BeNull();
    }

    [Fact]
    public async Task ExecuteAsync_ConcurrentModification_ReturnsFailure()
    {
        var message = CreateMessage();
        var ticket = new Ticket { Id = 1, Status = TicketStatus.Available };
        _ticketRepository.GetByIdAsync(1, Arg.Any<CancellationToken>())
            .Returns(ticket);
        _ticketRepository.TryReserveAsync(ticket, "user-456", "order-123", Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns(false);

        var result = await _useCase.ExecuteAsync(message);

        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("modified by another process");
    }

    [Fact]
    public async Task ExecuteAsync_ValidReservation_CallsRepositoryWithCorrectParameters()
    {
        var message = CreateMessage(ticketId: 42);
        var ticket = new Ticket { Id = 42, Status = TicketStatus.Available, Version = 5 };
        _ticketRepository.GetByIdAsync(42, Arg.Any<CancellationToken>())
            .Returns(ticket);
        _ticketRepository.TryReserveAsync(Arg.Any<Ticket>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns(true);

        await _useCase.ExecuteAsync(message);

        await _ticketRepository.Received(1).TryReserveAsync(
            Arg.Is<Ticket>(t => t.Id == 42),
            Arg.Is<string>(r => r == "user-456"),
            Arg.Is<string>(o => o == "order-123"),
            Arg.Any<DateTime>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_ReservationDuration_CalculatesExpiresAtCorrectly()
    {
        var beforeCall = DateTime.UtcNow;
        var message = CreateMessage();
        message.ReservationDurationSeconds = 600;
        var ticket = new Ticket { Id = 1, Status = TicketStatus.Available };
        _ticketRepository.GetByIdAsync(1, Arg.Any<CancellationToken>())
            .Returns(ticket);
        
        DateTime? capturedExpiresAt = null;
        _ticketRepository.TryReserveAsync(Arg.Any<Ticket>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Do<DateTime>(d => capturedExpiresAt = d), Arg.Any<CancellationToken>())
            .Returns(true);

        await _useCase.ExecuteAsync(message);

        capturedExpiresAt.Should().NotBeNull();
        var expectedExpiresAt = beforeCall.AddSeconds(600);
        capturedExpiresAt!.Value.Should().BeCloseTo(expectedExpiresAt, TimeSpan.FromSeconds(5));
    }

    [Theory]
    [InlineData(TicketStatus.Available, true)]
    [InlineData(TicketStatus.Reserved, false)]
    [InlineData(TicketStatus.Paid, false)]
    [InlineData(TicketStatus.Released, false)]
    [InlineData(TicketStatus.Cancelled, false)]
    public async Task ExecuteAsync_AllTicketStatuses_BehavesCorrectly(TicketStatus status, bool shouldSucceed)
    {
        var message = CreateMessage();
        var ticket = new Ticket { Id = 1, Status = status };
        _ticketRepository.GetByIdAsync(1, Arg.Any<CancellationToken>())
            .Returns(ticket);

        if (shouldSucceed)
        {
            _ticketRepository.TryReserveAsync(Arg.Any<Ticket>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
                .Returns(true);
        }

        var result = await _useCase.ExecuteAsync(message);

        result.Success.Should().Be(shouldSucceed);
    }
}
