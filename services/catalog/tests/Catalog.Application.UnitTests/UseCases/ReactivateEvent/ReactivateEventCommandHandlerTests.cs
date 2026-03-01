using Catalog.Application.Ports;
using Catalog.Application.UseCases.ReactivateEvent;
using Catalog.Domain.Entities;
using FluentAssertions;
using Moq;
using System.Reflection;

namespace Catalog.Application.UnitTests.UseCases.ReactivateEvent;

public class ReactivateEventCommandHandlerTests
{
    private readonly Mock<ICatalogRepository> _mockRepository;
    private readonly ReactivateEventCommandHandler _handler;

    public ReactivateEventCommandHandlerTests()
    {
        _mockRepository = new Mock<ICatalogRepository>();
        _handler = new ReactivateEventCommandHandler(_mockRepository.Object);
    }

    #region Reactivate Event Command Handler Tests - T106

    [Fact]
    public async Task Handle_WithValidCommand_ShouldReactivateEventSuccessfully()
    {
        // Arrange - Following Gherkin: "Reactivar evento desactivado"
        var eventId = Guid.NewGuid();
        var existingEvent = Event.Create(
            "Test Event",
            "Description",
            DateTime.UtcNow.AddDays(30), // Future date
            "Venue",
            1000,
            100m);

        // Simulate inactive event with unavailable seats - add seats first
        for (int i = 1; i <= 3; i++)
        {
            existingEvent.Seats.Add(new Seat
            {
                Id = Guid.NewGuid(),
                EventId = eventId,
                SectionCode = "General",
                RowNumber = 1,
                SeatNumber = i,
                Price = 100m,
                Status = Seat.StatusAvailable // Start as available
            });
        }
        
        // Now deactivate the event, which will make available seats unavailable
        existingEvent.Deactivate();

        var command = new ReactivateEventCommand(eventId);

        _mockRepository
            .Setup(r => r.GetEventWithSeatsAsync(eventId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingEvent);

        _mockRepository
            .Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert - Following Gherkin: "evento vuelve a estado 'active'"
        result.Should().NotBeNull();
        result.Id.Should().Be(existingEvent.Id);
        result.Status.Should().Be("active");
        result.Success.Should().BeTrue();
        result.UpdatedAt.Should().NotBeNull();

        // Verify repository calls
        _mockRepository.Verify(r => r.GetEventWithSeatsAsync(eventId, It.IsAny<CancellationToken>()), Times.Once);
        _mockRepository.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WithPastEventDate_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var existingEvent = Event.Create(
            "Past Event",
            "Description",
            DateTime.UtcNow.AddDays(1), // Future date for creation
            "Venue",
            1000,
            100m);

        existingEvent.Deactivate(); // Properly deactivate the event
        
        // Use reflection to set past date to bypass domain validation during creation
        var eventDateField = typeof(Event).GetProperty("EventDate");
        eventDateField?.SetValue(existingEvent, DateTime.UtcNow.AddDays(-1));

        var command = new ReactivateEventCommand(eventId);

        _mockRepository
            .Setup(r => r.GetEventWithSeatsAsync(eventId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingEvent);

        // Act & Assert
        var action = async () => await _handler.Handle(command, CancellationToken.None);
        await action.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("No se puede reactivar un evento que ya pasó");

        // Verify repository calls
        _mockRepository.Verify(r => r.GetEventWithSeatsAsync(eventId, It.IsAny<CancellationToken>()), Times.Once);
        _mockRepository.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WithNonExistentEvent_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var command = new ReactivateEventCommand(eventId);

        _mockRepository
            .Setup(r => r.GetEventWithSeatsAsync(eventId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Event?)null);

        // Act & Assert
        var action = async () => await _handler.Handle(command, CancellationToken.None);
        await action.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage($"Event with ID {eventId} not found");

        // Verify repository calls
        _mockRepository.Verify(r => r.GetEventWithSeatsAsync(eventId, It.IsAny<CancellationToken>()), Times.Once);
        _mockRepository.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    #endregion
}