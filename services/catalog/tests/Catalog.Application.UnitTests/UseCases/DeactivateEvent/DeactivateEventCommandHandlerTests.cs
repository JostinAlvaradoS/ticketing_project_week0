using Catalog.Application.Ports;
using Catalog.Application.UseCases.DeactivateEvent;
using Catalog.Domain.Entities;
using FluentAssertions;
using Moq;

namespace Catalog.Application.UnitTests.UseCases.DeactivateEvent;

public class DeactivateEventCommandHandlerTests
{
    private readonly Mock<ICatalogRepository> _mockRepository;
    private readonly DeactivateEventCommandHandler _handler;

    public DeactivateEventCommandHandlerTests()
    {
        _mockRepository = new Mock<ICatalogRepository>();
        _handler = new DeactivateEventCommandHandler(_mockRepository.Object);
    }

    #region Deactivate Event Command Handler Tests - T106

    [Fact]
    public async Task Handle_WithValidCommand_ShouldDeactivateEventSuccessfully()
    {
        // Arrange - Following Gherkin: "Desactivar evento (Soft Delete)"
        var eventId = Guid.NewGuid();
        var existingEvent = Event.Create(
            "Test Event",
            "Description", 
            DateTime.UtcNow.AddDays(30),
            "Venue",
            1000,
            100m);

        // Add available seats
        for (int i = 1; i <= 5; i++)
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

        var command = new DeactivateEventCommand(eventId);

        _mockRepository
            .Setup(r => r.GetEventWithSeatsAsync(eventId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingEvent);

        _mockRepository
            .Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert - Following Gherkin: "evento cambia a estado 'inactive'"
        result.Should().NotBeNull();
        result.Id.Should().Be(existingEvent.Id);
        result.Status.Should().Be("inactive");
        result.Success.Should().BeTrue();
        result.UpdatedAt.Should().NotBeNull();

        // Verify repository calls
        _mockRepository.Verify(r => r.GetEventWithSeatsAsync(eventId, It.IsAny<CancellationToken>()), Times.Once);
        _mockRepository.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WithActiveReservations_ShouldThrowInvalidOperationException()
    {
        // Arrange - Following Gherkin: "Intentar desactivar evento con reservas activas"
        var eventId = Guid.NewGuid();
        var existingEvent = Event.Create(
            "Test Event",
            "Description",
            DateTime.UtcNow.AddDays(30),
            "Venue", 
            1000,
            100m);

        // Add reserved seats ("el evento tiene 5 reservas activas")
        for (int i = 1; i <= 5; i++)
        {
            existingEvent.Seats.Add(new Seat
            {
                Id = Guid.NewGuid(),
                EventId = eventId,
                SectionCode = "General",
                RowNumber = 1,
                SeatNumber = i,
                Price = 100m,
                Status = Seat.StatusReserved
            });
        }

        var command = new DeactivateEventCommand(eventId);

        _mockRepository
            .Setup(r => r.GetEventWithSeatsAsync(eventId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingEvent);

        // Act & Assert
        var action = async () => await _handler.Handle(command, CancellationToken.None);
        await action.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("No se puede desactivar un evento con reservas activas");

        // Verify repository calls
        _mockRepository.Verify(r => r.GetEventWithSeatsAsync(eventId, It.IsAny<CancellationToken>()), Times.Once);
        _mockRepository.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WithNonExistentEvent_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var command = new DeactivateEventCommand(eventId);

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