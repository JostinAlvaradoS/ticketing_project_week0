using Catalog.Application.Ports;
using Catalog.Application.UseCases.GetEventSeatmap;
using Catalog.Domain.Entities;
using FluentAssertions;
using Moq;

namespace Catalog.Application.UnitTests.UseCases.GetEventSeatmap;

public class GetEventSeatmapHandlerTests
{
    private readonly Mock<ICatalogRepository> _mockRepository;
    private readonly GetEventSeatmapHandler _handler;

    public GetEventSeatmapHandlerTests()
    {
        _mockRepository = new Mock<ICatalogRepository>();
        _handler = new GetEventSeatmapHandler(_mockRepository.Object);
    }

    [Fact]
    public async Task Handle_WithExistingEventAndSeats_ShouldReturnEventSeatmapResponse()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var cancellationToken = CancellationToken.None;
        var query = new GetEventSeatmapQuery(eventId);

        var eventEntity = CreateEventWithSeats(eventId);

        _mockRepository
            .Setup(r => r.GetEventWithSeatsAsync(eventId, cancellationToken))
            .ReturnsAsync(eventEntity);

        // Act
        var result = await _handler.Handle(query, cancellationToken);

        // Assert
        result.Should().NotBeNull();
        result!.EventId.Should().Be(eventEntity.Id);
        result.EventName.Should().Be(eventEntity.Name);
        result.EventDescription.Should().Be(eventEntity.Description);
        result.EventDate.Should().Be(eventEntity.EventDate);
        result.BasePrice.Should().Be(eventEntity.BasePrice);
        
        result.Seats.Should().HaveCount(3);
        _mockRepository.Verify(r => r.GetEventWithSeatsAsync(eventId, cancellationToken), Times.Once);
    }

    [Fact]
    public async Task Handle_WithSeats_ShouldReturnSeatsOrderedCorrectly()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var cancellationToken = CancellationToken.None;
        var query = new GetEventSeatmapQuery(eventId);

        var eventEntity = CreateEventWithMixedSeats(eventId);

        _mockRepository
            .Setup(r => r.GetEventWithSeatsAsync(eventId, cancellationToken))
            .ReturnsAsync(eventEntity);

        // Act
        var result = await _handler.Handle(query, cancellationToken);

        // Assert
        result.Should().NotBeNull();
        
        var seats = result!.Seats.ToList();
        seats.Should().HaveCount(4);
        
        // Should be ordered by SectionCode, then RowNumber, then SeatNumber
        seats[0].SectionCode.Should().Be("General");
        seats[0].RowNumber.Should().Be(1);
        seats[0].SeatNumber.Should().Be(1);
        
        seats[1].SectionCode.Should().Be("General");
        seats[1].RowNumber.Should().Be(1);
        seats[1].SeatNumber.Should().Be(2);
        
        seats[2].SectionCode.Should().Be("VIP");
        seats[2].RowNumber.Should().Be(1);
        seats[2].SeatNumber.Should().Be(1);
        
        seats[3].SectionCode.Should().Be("VIP");
        seats[3].RowNumber.Should().Be(2);
        seats[3].SeatNumber.Should().Be(1);
    }

    [Fact]
    public async Task Handle_WithNonExistentEvent_ShouldReturnNull()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var cancellationToken = CancellationToken.None;
        var query = new GetEventSeatmapQuery(eventId);

        _mockRepository
            .Setup(r => r.GetEventWithSeatsAsync(eventId, cancellationToken))
            .ReturnsAsync((Event?)null);

        // Act
        var result = await _handler.Handle(query, cancellationToken);

        // Assert
        result.Should().BeNull();
        _mockRepository.Verify(r => r.GetEventWithSeatsAsync(eventId, cancellationToken), Times.Once);
    }

    [Fact]
    public async Task Handle_WithEventButNoSeats_ShouldReturnResponseWithEmptySeats()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var cancellationToken = CancellationToken.None;
        var query = new GetEventSeatmapQuery(eventId);

        var eventEntity = CreateEventWithoutSeats(eventId);

        _mockRepository
            .Setup(r => r.GetEventWithSeatsAsync(eventId, cancellationToken))
            .ReturnsAsync(eventEntity);

        // Act
        var result = await _handler.Handle(query, cancellationToken);

        // Assert
        result.Should().NotBeNull();
        result!.EventId.Should().Be(eventId);
        result.Seats.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_WithSeats_ShouldMapSeatDataCorrectly()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var cancellationToken = CancellationToken.None;
        var query = new GetEventSeatmapQuery(eventId);

        var eventEntity = CreateEventWithSpecificSeat(eventId);

        _mockRepository
            .Setup(r => r.GetEventWithSeatsAsync(eventId, cancellationToken))
            .ReturnsAsync(eventEntity);

        // Act
        var result = await _handler.Handle(query, cancellationToken);

        // Assert
        result.Should().NotBeNull();
        
        var seatDto = result!.Seats.Single();
        var originalSeat = eventEntity.Seats.Single();
        
        seatDto.Id.Should().Be(originalSeat.Id);
        seatDto.SectionCode.Should().Be(originalSeat.SectionCode);
        seatDto.RowNumber.Should().Be(originalSeat.RowNumber);
        seatDto.SeatNumber.Should().Be(originalSeat.SeatNumber);
        seatDto.Price.Should().Be(originalSeat.Price);
        seatDto.Status.Should().Be(originalSeat.Status);
    }

    [Fact]
    public async Task Handle_WithRepositoryException_ShouldPropagateException()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var cancellationToken = CancellationToken.None;
        var query = new GetEventSeatmapQuery(eventId);

        var expectedException = new InvalidOperationException("Database error");
        _mockRepository
            .Setup(r => r.GetEventWithSeatsAsync(eventId, cancellationToken))
            .ThrowsAsync(expectedException);

        // Act & Assert
        var action = async () => await _handler.Handle(query, cancellationToken);
        await action.Should().ThrowAsync<InvalidOperationException>()
                   .WithMessage("Database error");

        _mockRepository.Verify(r => r.GetEventWithSeatsAsync(eventId, cancellationToken), Times.Once);
    }

    [Fact]
    public async Task Handle_WithCancellationToken_ShouldPassTokenToRepository()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        using var cancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = cancellationTokenSource.Token;
        var query = new GetEventSeatmapQuery(eventId);

        _mockRepository
            .Setup(r => r.GetEventWithSeatsAsync(eventId, cancellationToken))
            .ReturnsAsync((Event?)null);

        // Act
        await _handler.Handle(query, cancellationToken);

        // Assert
        _mockRepository.Verify(r => r.GetEventWithSeatsAsync(eventId, cancellationToken), Times.Once);
    }

    [Fact]
    public async Task Handle_WithSeatsHavingDifferentStatuses_ShouldPreserveAllStatuses()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var cancellationToken = CancellationToken.None;
        var query = new GetEventSeatmapQuery(eventId);

        var eventEntity = CreateEventWithSeatsInDifferentStatuses(eventId);

        _mockRepository
            .Setup(r => r.GetEventWithSeatsAsync(eventId, cancellationToken))
            .ReturnsAsync(eventEntity);

        // Act
        var result = await _handler.Handle(query, cancellationToken);

        // Assert
        result.Should().NotBeNull();
        
        var seats = result!.Seats.ToList();
        seats.Should().HaveCount(3);
        
        seats.Should().Contain(s => s.Status == Seat.StatusAvailable);
        seats.Should().Contain(s => s.Status == Seat.StatusReserved);
        seats.Should().Contain(s => s.Status == Seat.StatusSold);
    }

    // Test Helper Methods
    private static Event CreateEventWithSeats(Guid eventId)
    {
        var eventEntity = Event.Create(
            "Test Concert with Seats",
            "A concert with seats",
            DateTime.UtcNow.AddDays(15),
            "Test Venue",
            1000,
            100.00m);

        // Use reflection to set ID for testing
        typeof(Event).GetProperty("Id")?.SetValue(eventEntity, eventId);

        eventEntity.Seats.Add(CreateSeat(eventId, "VIP", 1, 1, 200m, Seat.StatusAvailable));
        eventEntity.Seats.Add(CreateSeat(eventId, "General", 1, 1, 100m, Seat.StatusAvailable));
        eventEntity.Seats.Add(CreateSeat(eventId, "General", 1, 2, 100m, Seat.StatusReserved));

        return eventEntity;
    }

    private static Event CreateEventWithMixedSeats(Guid eventId)
    {
        var eventEntity = Event.Create(
            "Mixed Seating Event",
            "Event with various seat sections",
            DateTime.UtcNow.AddDays(20),
            "Test Venue",
            1000,
            75.00m);

        // Use reflection to set ID for testing
        typeof(Event).GetProperty("Id")?.SetValue(eventEntity, eventId);

        // Adding in random order to test sorting
        eventEntity.Seats.Add(CreateSeat(eventId, "VIP", 2, 1, 250m, Seat.StatusAvailable));
        eventEntity.Seats.Add(CreateSeat(eventId, "General", 1, 2, 100m, Seat.StatusAvailable));
        eventEntity.Seats.Add(CreateSeat(eventId, "VIP", 1, 1, 200m, Seat.StatusAvailable));
        eventEntity.Seats.Add(CreateSeat(eventId, "General", 1, 1, 100m, Seat.StatusAvailable));

        return eventEntity;
    }

    private static Event CreateEventWithoutSeats(Guid eventId)
    {
        var eventEntity = Event.Create(
            "Event Without Seats",
            "No seats available",
            DateTime.UtcNow.AddDays(10),
            "Test Venue",
            1000,
            50.00m);

        // Use reflection to set ID for testing
        typeof(Event).GetProperty("Id")?.SetValue(eventEntity, eventId);
        
        return eventEntity;
    }

    private static Event CreateEventWithSpecificSeat(Guid eventId)
    {
        var eventEntity = Event.Create(
            "Single Seat Event",
            "Event with one specific seat",
            DateTime.UtcNow.AddDays(25),
            "Test Venue",
            1000,
            150.00m);

        // Use reflection to set ID for testing
        typeof(Event).GetProperty("Id")?.SetValue(eventEntity, eventId);

        eventEntity.Seats.Add(CreateSeat(eventId, "Premium", 5, 12, 175.50m, Seat.StatusReserved));

        return eventEntity;
    }

    private static Event CreateEventWithSeatsInDifferentStatuses(Guid eventId)
    {
        var eventEntity = Event.Create(
            "Status Mix Event",
            "Event with seats in different statuses",
            DateTime.UtcNow.AddDays(12),
            "Test Venue",
            1000,
            80.00m);

        // Use reflection to set ID for testing
        typeof(Event).GetProperty("Id")?.SetValue(eventEntity, eventId);

        eventEntity.Seats.Add(CreateSeat(eventId, "A", 1, 1, 80m, Seat.StatusAvailable));
        eventEntity.Seats.Add(CreateSeat(eventId, "A", 1, 2, 80m, Seat.StatusReserved));
        eventEntity.Seats.Add(CreateSeat(eventId, "A", 1, 3, 80m, Seat.StatusSold));

        return eventEntity;
    }

    private static Seat CreateSeat(Guid eventId, string section, int row, int seatNumber, decimal price, string status)
    {
        return new Seat
        {
            Id = Guid.NewGuid(),
            EventId = eventId,
            SectionCode = section,
            RowNumber = row,
            SeatNumber = seatNumber,
            Price = price,
            Status = status
        };
    }
}