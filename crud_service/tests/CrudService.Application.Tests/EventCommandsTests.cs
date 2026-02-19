using CrudService.Application.Dtos;
using CrudService.Application.Ports.Outbound;
using CrudService.Application.UseCases.Commands;
using CrudService.Domain.Entities;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace CrudService.Application.Tests;

public class EventCommandsTests
{
    private readonly IEventRepository _eventRepository;
    private readonly EventCommands _sut;

    public EventCommandsTests()
    {
        _eventRepository = Substitute.For<IEventRepository>();
        _sut = new EventCommands(_eventRepository);
    }

    [Fact]
    public async Task CreateEventAsync_ShouldCreateEvent_WhenValidRequest()
    {
        var request = new CreateEventRequest
        {
            Name = "Test Event",
            StartsAt = DateTime.UtcNow.AddDays(7)
        };
        var createdEvent = new Event
        {
            Id = 1,
            Name = request.Name,
            StartsAt = request.StartsAt
        };
        _eventRepository.AddAsync(Arg.Any<Event>()).Returns(createdEvent);

        var result = await _sut.CreateEventAsync(request);

        result.Name.Should().Be(request.Name);
        await _eventRepository.Received(1).AddAsync(Arg.Any<Event>());
    }

    [Fact]
    public async Task UpdateEventAsync_ShouldUpdateEvent_WhenEventExists()
    {
        var eventId = 1L;
        var existingEvent = new Event
        {
            Id = eventId,
            Name = "Old Name",
            StartsAt = DateTime.UtcNow
        };
        var request = new UpdateEventRequest
        {
            Name = "New Name"
        };
        _eventRepository.GetByIdAsync(eventId).Returns(existingEvent);
        _eventRepository.UpdateAsync(Arg.Any<Event>()).Returns(x => x.Arg<Event>());

        var result = await _sut.UpdateEventAsync(eventId, request);

        result.Name.Should().Be("New Name");
    }

    [Fact]
    public async Task UpdateEventAsync_ShouldThrow_WhenEventNotFound()
    {
        var eventId = 999L;
        _eventRepository.GetByIdAsync(eventId).Returns((Event?)null);
        var request = new UpdateEventRequest { Name = "New Name" };

        var act = () => _sut.UpdateEventAsync(eventId, request);

        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task DeleteEventAsync_ShouldDeleteEvent_WhenEventExists()
    {
        var eventId = 1L;
        _eventRepository.DeleteAsync(eventId).Returns(true);

        var result = await _sut.DeleteEventAsync(eventId);

        result.Should().BeTrue();
    }

    [Fact]
    public async Task DeleteEventAsync_ShouldReturnFalse_WhenEventNotFound()
    {
        var eventId = 999L;
        _eventRepository.DeleteAsync(eventId).Returns(false);

        var result = await _sut.DeleteEventAsync(eventId);

        result.Should().BeFalse();
    }
}
