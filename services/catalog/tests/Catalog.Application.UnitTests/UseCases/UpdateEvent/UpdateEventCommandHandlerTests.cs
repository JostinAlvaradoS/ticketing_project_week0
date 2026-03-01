using Catalog.Application.Ports;
using Catalog.Application.UseCases.UpdateEvent;
using Catalog.Domain.Entities;
using FluentAssertions;
using Moq;

namespace Catalog.Application.UnitTests.UseCases.UpdateEvent;

public class UpdateEventCommandHandlerTests
{
    private readonly Mock<ICatalogRepository> _mockRepository;
    private readonly UpdateEventCommandHandler _handler;

    public UpdateEventCommandHandlerTests()
    {
        _mockRepository = new Mock<ICatalogRepository>();
        _handler = new UpdateEventCommandHandler(_mockRepository.Object);
    }

    #region Update Event Command Handler Tests - T106

    [Fact]
    public async Task Handle_WithValidCommand_ShouldUpdateEventSuccessfully()
    {
        // Arrange - Following Gherkin: "Actualizar información de evento existente"
        var eventId = Guid.NewGuid();
        var existingEvent = Event.Create(
            "Original Name",
            "Original Description", 
            DateTime.UtcNow.AddDays(30),
            "Original Venue",
            1000,
            100m);

        var command = new UpdateEventCommand(
            eventId,
            "Concierto Foo Fighters 2026 - SOLD OUT",
            "Evento agotado - últimas entradas",
            45000);

        _mockRepository
            .Setup(r => r.GetEventWithSeatsAsync(eventId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingEvent);

        _mockRepository
            .Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert - Following Gherkin: "los cambios se persisten"
        result.Should().NotBeNull();
        result.Id.Should().Be(existingEvent.Id);
        result.Name.Should().Be(command.Name);
        result.Description.Should().Be(command.Description);
        result.MaxCapacity.Should().Be(command.MaxCapacity);
        result.UpdatedAt.Should().NotBeNull();

        // Verify repository calls
        _mockRepository.Verify(r => r.GetEventWithSeatsAsync(eventId, It.IsAny<CancellationToken>()), Times.Once);
        _mockRepository.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WithNonExistentEvent_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var command = new UpdateEventCommand(
            eventId,
            "Updated Name",
            "Updated Description",
            1000);

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

    [Fact]
    public async Task Handle_ReducingCapacityBelowSeatCount_ShouldThrowException()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var existingEvent = Event.Create(
            "Test Event",
            "Description",
            DateTime.UtcNow.AddDays(30),
            "Venue",
            1000,
            100m);

        // Add 10 seats to the event
        for (int i = 1; i <= 10; i++)
        {
            existingEvent.Seats.Add(new Seat
            {
                Id = Guid.NewGuid(),
                EventId = eventId,
                SectionCode = "General",
                RowNumber = 1,
                SeatNumber = i,
                Price = 100m,
                Status = Seat.StatusAvailable
            });
        }

        var command = new UpdateEventCommand(
            eventId,
            "Updated Name",
            "Updated Description",
            5); // Try to reduce capacity below seat count

        _mockRepository
            .Setup(r => r.GetEventWithSeatsAsync(eventId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingEvent);

        // Act & Assert
        var action = async () => await _handler.Handle(command, CancellationToken.None);
        await action.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("No se puede reducir la capacidad por debajo del número de asientos existentes");

        // Verify repository calls
        _mockRepository.Verify(r => r.GetEventWithSeatsAsync(eventId, It.IsAny<CancellationToken>()), Times.Once);
        _mockRepository.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    #endregion
}