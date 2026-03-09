using Catalog.Application.Ports;
using Catalog.Application.UseCases.GetAllEvents;
using Catalog.Domain.Entities;
using FluentAssertions;
using Moq;

namespace Catalog.Application.UnitTests.UseCases.GetAllEvents;

public class GetAllEventsHandlerTests
{
    private readonly Mock<ICatalogRepository> _mockRepository;
    private readonly GetAllEventsHandler _handler;

    public GetAllEventsHandlerTests()
    {
        _mockRepository = new Mock<ICatalogRepository>();
        _handler = new GetAllEventsHandler(_mockRepository.Object);
    }

    [Fact]
    public async Task Handle_WithExistingEvents_ShouldReturnEventDtosOrderedByEventDateDescending()
    {
        // Arrange
        var cancellationToken = CancellationToken.None;
        var query = new GetAllEventsQuery();

        var events = new List<Event>
        {
            CreateEvent("Event A", DateTime.UtcNow.AddDays(10), 100m),
            CreateEvent("Event B", DateTime.UtcNow.AddDays(30), 150m),
            CreateEvent("Event C", DateTime.UtcNow.AddDays(5), 75m)
        };

        _mockRepository
            .Setup(r => r.GetAllEventsWithSeatsAsync(cancellationToken))
            .ReturnsAsync(events);

        // Act
        var result = await _handler.Handle(query, cancellationToken);

        // Assert
        result.Should().HaveCount(3);
        result.Should().BeInDescendingOrder(e => e.EventDate);
        
        result.First().Name.Should().Be("Event B"); // Latest date (30 days)
        result.Last().Name.Should().Be("Event C");  // Earliest date (5 days)
        
        _mockRepository.Verify(r => r.GetAllEventsWithSeatsAsync(cancellationToken), Times.Once);
    }

    [Fact]
    public async Task Handle_WithNoEvents_ShouldReturnEmptyCollection()
    {
        // Arrange
        var cancellationToken = CancellationToken.None;
        var query = new GetAllEventsQuery();

        _mockRepository
            .Setup(r => r.GetAllEventsWithSeatsAsync(cancellationToken))
            .ReturnsAsync(new List<Event>());

        // Act
        var result = await _handler.Handle(query, cancellationToken);

        // Assert
        result.Should().BeEmpty();
        _mockRepository.Verify(r => r.GetAllEventsWithSeatsAsync(cancellationToken), Times.Once);
    }

    [Fact]
    public async Task Handle_WithSingleEvent_ShouldReturnCorrectEventDto()
    {
        // Arrange
        var cancellationToken = CancellationToken.None;
        var query = new GetAllEventsQuery();
        
        var eventEntity = CreateEvent("Test Concert", DateTime.UtcNow.AddDays(15), 125.50m);
        var events = new List<Event> { eventEntity };

        _mockRepository
            .Setup(r => r.GetAllEventsWithSeatsAsync(cancellationToken))
            .ReturnsAsync(events);

        // Act
        var result = await _handler.Handle(query, cancellationToken);

        // Assert
        var eventDto = result.Single();
        eventDto.Id.Should().Be(eventEntity.Id);
        eventDto.Name.Should().Be(eventEntity.Name);
        eventDto.Description.Should().Be(eventEntity.Description);
        eventDto.EventDate.Should().Be(eventEntity.EventDate);
        eventDto.BasePrice.Should().Be(eventEntity.BasePrice);
    }

    [Fact]
    public async Task Handle_WithRepositoryException_ShouldPropagateException()
    {
        // Arrange
        var cancellationToken = CancellationToken.None;
        var query = new GetAllEventsQuery();

        var expectedException = new InvalidOperationException("Database error");
        _mockRepository
            .Setup(r => r.GetAllEventsWithSeatsAsync(cancellationToken))
            .ThrowsAsync(expectedException);

        // Act & Assert
        var action = async () => await _handler.Handle(query, cancellationToken);
        await action.Should().ThrowAsync<InvalidOperationException>()
                   .WithMessage("Database error");
        
        _mockRepository.Verify(r => r.GetAllEventsWithSeatsAsync(cancellationToken), Times.Once);
    }

    [Fact]
    public async Task Handle_WithCancellationToken_ShouldPassTokenToRepository()
    {
        // Arrange
        using var cancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = cancellationTokenSource.Token;
        var query = new GetAllEventsQuery();

        _mockRepository
            .Setup(r => r.GetAllEventsWithSeatsAsync(cancellationToken))
            .ReturnsAsync(new List<Event>());

        // Act
        await _handler.Handle(query, cancellationToken);

        // Assert
        _mockRepository.Verify(r => r.GetAllEventsWithSeatsAsync(cancellationToken), Times.Once);
    }

    [Fact]
    public async Task Handle_WithEventsHavingSameDates_ShouldMaintainStableOrder()
    {
        // Arrange
        var cancellationToken = CancellationToken.None;
        var query = new GetAllEventsQuery();
        
        var sameDate = DateTime.UtcNow.AddDays(15);
        var events = new List<Event>
        {
            CreateEvent("Event X", sameDate, 100m),
            CreateEvent("Event Y", sameDate, 150m),
            CreateEvent("Event Z", sameDate, 200m)
        };

        _mockRepository
            .Setup(r => r.GetAllEventsWithSeatsAsync(cancellationToken))
            .ReturnsAsync(events);

        // Act
        var result = await _handler.Handle(query, cancellationToken);

        // Assert
        result.Should().HaveCount(3);
        result.Should().OnlyContain(e => e.EventDate == sameDate);
    }

    [Fact]
    public async Task Handle_Should_Filter_Out_Past_Events_And_Inactive_Events()
    {
        // Arrange (TC-P2-01: Start_Date > Now)
        var cancellationToken = CancellationToken.None;
        var query = new GetAllEventsQuery();
        var now = DateTime.UtcNow;

        var futureActive = CreateEvent("Future Event", now.AddDays(1), 100m);
        
        // Simular un evento pasado sin usar el Factory que valida la fecha
        var pastEvent = (Event)System.Runtime.Serialization.FormatterServices.GetUninitializedObject(typeof(Event));
        typeof(Event).GetProperty("Id")?.SetValue(pastEvent, Guid.NewGuid());
        typeof(Event).GetProperty("Name")?.SetValue(pastEvent, "Past Event");
        typeof(Event).GetProperty("EventDate")?.SetValue(pastEvent, now.AddDays(-1));
        typeof(Event).GetProperty("Status")?.SetValue(pastEvent, "active");

        var events = new List<Event> { futureActive, pastEvent };

        _mockRepository
            .Setup(r => r.GetAllEventsWithSeatsAsync(cancellationToken))
            .ReturnsAsync(events);

        // Act
        var result = await _handler.Handle(query, cancellationToken);

        // Assert
        result.Should().HaveCount(1);
        result.Single().Name.Should().Be("Future Event");
    }

    // Test Helper Methods
    private static Event CreateEvent(string name, DateTime eventDate, decimal basePrice)
    {
        return Event.Create(
            name,
            $"Description for {name}",
            eventDate,
            "Test Venue",
            1000,
            basePrice);
    }
}