using CrudService.Application.Dtos;
using CrudService.Application.Ports.Outbound;
using CrudService.Application.UseCases.Commands;
using CrudService.Domain.Entities;
using CrudService.Domain.Enums;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace CrudService.Application.Tests;

public class TicketCommandsTests
{
    private readonly ITicketRepository _ticketRepository;
    private readonly ITicketHistoryRepository _historyRepository;
    private readonly TicketCommands _sut;

    public TicketCommandsTests()
    {
        _ticketRepository = Substitute.For<ITicketRepository>();
        _historyRepository = Substitute.For<ITicketHistoryRepository>();
        _sut = new TicketCommands(_ticketRepository, _historyRepository);
    }

    [Fact]
    public async Task CreateTicketsAsync_ShouldCreateTickets_WhenValidEventId()
    {
        var eventId = 1L;
        var quantity = 3;
        var createdTickets = new List<Ticket>
        {
            new() { Id = 1, EventId = eventId, Status = TicketStatus.Available },
            new() { Id = 2, EventId = eventId, Status = TicketStatus.Available },
            new() { Id = 3, EventId = eventId, Status = TicketStatus.Available }
        };

        var callCount = 0;
        _ticketRepository.AddAsync(Arg.Any<Ticket>()).Returns(x =>
        {
            callCount++;
            return createdTickets[callCount - 1];
        });

        var result = await _sut.CreateTicketsAsync(eventId, quantity);

        result.Should().HaveCount(quantity);
        await _ticketRepository.Received(quantity).AddAsync(Arg.Any<Ticket>());
    }

    [Fact]
    public async Task UpdateTicketStatusAsync_ShouldUpdateStatus_WhenTicketExists()
    {
        var ticketId = 1L;
        var ticket = new Ticket
        {
            Id = ticketId,
            EventId = 1,
            Status = TicketStatus.Available,
            Version = 0
        };
        _ticketRepository.GetByIdAsync(ticketId).Returns(ticket);
        _ticketRepository.UpdateAsync(Arg.Any<Ticket>()).Returns(x => x.Arg<Ticket>());

        var result = await _sut.UpdateTicketStatusAsync(ticketId, "Reserved", "Test reason");

        result.Status.Should().Be("Reserved");
        result.Version.Should().Be(1);
        await _historyRepository.Received(1).AddAsync(Arg.Any<TicketHistory>());
    }

    [Fact]
    public async Task UpdateTicketStatusAsync_ShouldThrow_WhenTicketNotFound()
    {
        var ticketId = 999L;
        _ticketRepository.GetByIdAsync(ticketId).Returns((Ticket?)null);

        var act = () => _sut.UpdateTicketStatusAsync(ticketId, "Reserved");

        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task UpdateTicketStatusAsync_ShouldThrow_WhenInvalidStatus()
    {
        var ticketId = 1L;
        var ticket = new Ticket { Id = ticketId, Status = TicketStatus.Available };
        _ticketRepository.GetByIdAsync(ticketId).Returns(ticket);

        var act = () => _sut.UpdateTicketStatusAsync(ticketId, "InvalidStatus");

        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task ReleaseTicketAsync_ShouldReleaseTicket_WhenTicketExists()
    {
        var ticketId = 1L;
        var ticket = new Ticket
        {
            Id = ticketId,
            EventId = 1,
            Status = TicketStatus.Reserved,
            ReservedBy = "user@test.com",
            OrderId = "order-123",
            Version = 1
        };
        _ticketRepository.GetByIdAsync(ticketId).Returns(ticket);
        _ticketRepository.UpdateAsync(Arg.Any<Ticket>()).Returns(x => x.Arg<Ticket>());

        var result = await _sut.ReleaseTicketAsync(ticketId, "Test release");

        result.Status.Should().Be("Available");
        result.ReservedBy.Should().BeNull();
        result.OrderId.Should().BeNull();
        result.Version.Should().Be(2);
    }

    [Fact]
    public async Task ReleaseTicketAsync_ShouldThrow_WhenTicketNotFound()
    {
        var ticketId = 999L;
        _ticketRepository.GetByIdAsync(ticketId).Returns((Ticket?)null);

        var act = () => _sut.ReleaseTicketAsync(ticketId);

        await act.Should().ThrowAsync<KeyNotFoundException>();
    }
}
