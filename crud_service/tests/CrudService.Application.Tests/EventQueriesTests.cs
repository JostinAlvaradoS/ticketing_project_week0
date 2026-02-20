using CrudService.Application.Ports.Outbound;
using CrudService.Application.UseCases.Queries;
using CrudService.Domain.Entities;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace CrudService.Application.Tests;

public class EventQueriesTests
{
    private readonly IEventRepository _eventRepository;
    private readonly EventQueries _sut;

    public EventQueriesTests()
    {
        _eventRepository = Substitute.For<IEventRepository>();
        _sut = new EventQueries(_eventRepository);
    }

    [Fact]
    public async Task GetAllEventsAsync_ShouldReturnAllEvents()
    {
        var events = new List<Event>
        {
            new() { Id = 1, Name = "Event 1", StartsAt = DateTime.UtcNow },
            new() { Id = 2, Name = "Event 2", StartsAt = DateTime.UtcNow.AddDays(1) }
        };
        _eventRepository.GetAllAsync().Returns(events);

        var result = await _sut.GetAllEventsAsync();

        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetEventByIdAsync_ShouldReturnEvent_WhenEventExists()
    {
        var eventId = 1L;
        var eventEntity = new Event
        {
            Id = eventId,
            Name = "Test Event",
            StartsAt = DateTime.UtcNow
        };
        _eventRepository.GetByIdAsync(eventId).Returns(eventEntity);

        var result = await _sut.GetEventByIdAsync(eventId);

        result.Should().NotBeNull();
        result!.Id.Should().Be(eventId);
    }

    [Fact]
    public async Task GetEventByIdAsync_ShouldReturnNull_WhenEventNotFound()
    {
        var eventId = 999L;
        _eventRepository.GetByIdAsync(eventId).Returns((Event?)null);

        var result = await _sut.GetEventByIdAsync(eventId);

        result.Should().BeNull();
    }
}
