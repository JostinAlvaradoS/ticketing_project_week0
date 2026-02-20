using CrudService.Application.Ports.Outbound;
using CrudService.Application.UseCases.Queries;
using CrudService.Domain.Entities;
using CrudService.Domain.Enums;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace CrudService.Application.Tests;

public class TicketQueriesTests
{
    private readonly ITicketRepository _ticketRepository;
    private readonly TicketQueries _sut;

    public TicketQueriesTests()
    {
        _ticketRepository = Substitute.For<ITicketRepository>();
        _sut = new TicketQueries(_ticketRepository);
    }

    [Fact]
    public async Task GetTicketsByEventAsync_ShouldReturnTickets_WhenEventExists()
    {
        var eventId = 1L;
        var tickets = new List<Ticket>
        {
            new() { Id = 1, EventId = eventId, Status = TicketStatus.Available },
            new() { Id = 2, EventId = eventId, Status = TicketStatus.Reserved }
        };
        _ticketRepository.GetByEventIdAsync(eventId).Returns(tickets);

        var result = await _sut.GetTicketsByEventAsync(eventId);

        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetTicketByIdAsync_ShouldReturnTicket_WhenTicketExists()
    {
        var ticketId = 1L;
        var ticket = new Ticket
        {
            Id = ticketId,
            EventId = 1,
            Status = TicketStatus.Available
        };
        _ticketRepository.GetByIdAsync(ticketId).Returns(ticket);

        var result = await _sut.GetTicketByIdAsync(ticketId);

        result.Should().NotBeNull();
        result!.Id.Should().Be(ticketId);
    }

    [Fact]
    public async Task GetTicketByIdAsync_ShouldReturnNull_WhenTicketNotFound()
    {
        var ticketId = 999L;
        _ticketRepository.GetByIdAsync(ticketId).Returns((Ticket?)null);

        var result = await _sut.GetTicketByIdAsync(ticketId);

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetExpiredTicketsAsync_ShouldReturnExpiredTickets()
    {
        var expiredTickets = new List<Ticket>
        {
            new() { Id = 1, Status = TicketStatus.Reserved, ExpiresAt = DateTime.UtcNow.AddHours(-1) }
        };
        _ticketRepository.GetExpiredAsync(Arg.Any<DateTime>()).Returns(expiredTickets);

        var result = await _sut.GetExpiredTicketsAsync();

        result.Should().HaveCount(1);
    }
}
