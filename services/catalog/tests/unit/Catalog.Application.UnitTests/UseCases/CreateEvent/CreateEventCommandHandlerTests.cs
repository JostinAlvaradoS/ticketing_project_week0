using Catalog.Application.Ports;
using Catalog.Application.UseCases.CreateEvent;
using Catalog.Domain.Entities;
using FluentAssertions;
using Moq;

namespace Catalog.Application.UnitTests.UseCases.CreateEvent;

public class CreateEventCommandHandlerTests
{
    private readonly Mock<ICatalogRepository> _mockRepository;
    private readonly CreateEventCommandHandler _handler;

    public CreateEventCommandHandlerTests()
    {
        _mockRepository = new Mock<ICatalogRepository>();
        _handler = new CreateEventCommandHandler(_mockRepository.Object);
    }

    #region Command Handler Tests - Following Gherkin Scenarios (T102)

    [Fact]
    public async Task Handle_WithValidCommand_ShouldCreateEventSuccessfully()
    {
        // Arrange - Following Gherkin: "Procesar comando CreateEvent a través de MediatR"
        var command = new CreateEventCommand(
            "Concierto Foo Fighters 2026",
            "Concierto en el Estadio Nacional",
            DateTime.Parse("2026-06-15T20:00:00Z").ToUniversalTime(),
            "Estadio Nacional",
            50000,
            120.50m);

        var expectedEvent = Event.Create(
            command.Name,
            command.Description,
            command.EventDate,
            command.Venue,
            command.MaxCapacity,
            command.BasePrice);

        _mockRepository
            .Setup(r => r.CreateEventAsync(It.IsAny<Event>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedEvent);

        _mockRepository
            .Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert - Following Gherkin expectations
        result.Should().NotBeNull();
        result.Name.Should().Be(command.Name);
        result.Description.Should().Be(command.Description);
        result.EventDate.Should().Be(command.EventDate);
        result.Venue.Should().Be(command.Venue);
        result.MaxCapacity.Should().Be(command.MaxCapacity);
        result.BasePrice.Should().Be(command.BasePrice);
        result.Status.Should().Be("active");
        result.Id.Should().NotBeEmpty();
        result.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(2));

        // Verify repository interactions
        _mockRepository.Verify(r => r.CreateEventAsync(
            It.Is<Event>(e => e.Name == command.Name && 
                             e.Description == command.Description &&
                             e.EventDate == command.EventDate &&
                             e.Venue == command.Venue &&
                             e.MaxCapacity == command.MaxCapacity &&
                             e.BasePrice == command.BasePrice), 
            It.IsAny<CancellationToken>()), Times.Once);

        _mockRepository.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WithInvalidName_ShouldThrowArgumentException()
    {
        // Arrange - Following Gherkin: validation should happen in Event.Create()
        var command = new CreateEventCommand(
            "", // Invalid empty name
            "Valid description",
            DateTime.UtcNow.AddMonths(2),
            "Test Venue",
            1000,
            50.00m);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(
            () => _handler.Handle(command, CancellationToken.None));
        
        exception.Message.Should().Contain("El nombre del evento es obligatorio");
        
        // Repository should not be called when validation fails
        _mockRepository.Verify(r => r.CreateEventAsync(It.IsAny<Event>(), It.IsAny<CancellationToken>()), Times.Never);
        _mockRepository.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WithPastDate_ShouldThrowArgumentException()
    {
        // Arrange
        var command = new CreateEventCommand(
            "Valid Event",
            "Valid description",
            DateTime.Parse("2020-01-01T20:00:00Z"), // Past date
            "Test Venue",
            1000,
            50.00m);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(
            () => _handler.Handle(command, CancellationToken.None));
        
        exception.Message.Should().Contain("La fecha del evento debe ser futura");
        
        // Repository should not be called when validation fails
        _mockRepository.Verify(r => r.CreateEventAsync(It.IsAny<Event>(), It.IsAny<CancellationToken>()), Times.Never);
        _mockRepository.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WithNegativePrice_ShouldThrowArgumentException()
    {
        // Arrange
        var command = new CreateEventCommand(
            "Valid Event",
            "Valid description",
            DateTime.UtcNow.AddMonths(2),
            "Test Venue",
            1000,
            -10.00m); // Negative price

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(
            () => _handler.Handle(command, CancellationToken.None));
        
        exception.Message.Should().Contain("El precio base debe ser mayor a cero");
        
        // Repository should not be called when validation fails
        _mockRepository.Verify(r => r.CreateEventAsync(It.IsAny<Event>(), It.IsAny<CancellationToken>()), Times.Never);
        _mockRepository.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ShouldCallRepositoryInCorrectOrder()
    {
        // Arrange
        var command = CreateValidCommand();
        var expectedEvent = Event.Create(
            command.Name,
            command.Description,
            command.EventDate,
            command.Venue,
            command.MaxCapacity,
            command.BasePrice);

        _mockRepository
            .Setup(r => r.CreateEventAsync(It.IsAny<Event>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedEvent);

        _mockRepository
            .Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert - Verify the sequence of calls
        var sequence = new MockSequence();
        _mockRepository.InSequence(sequence)
            .Setup(r => r.CreateEventAsync(It.IsAny<Event>(), It.IsAny<CancellationToken>()));
        _mockRepository.InSequence(sequence)
            .Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()));
    }

    [Fact]
    public async Task Handle_WhenRepositoryThrows_ShouldPropagateException()
    {
        // Arrange
        var command = CreateValidCommand();
        var repositoryException = new InvalidOperationException("Database connection failed");

        _mockRepository
            .Setup(r => r.CreateEventAsync(It.IsAny<Event>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(repositoryException);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _handler.Handle(command, CancellationToken.None));
        
        exception.Should().Be(repositoryException);
        
        // SaveChanges should not be called if CreateEventAsync fails
        _mockRepository.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    #endregion

    #region Test Helper Methods

    private static CreateEventCommand CreateValidCommand()
    {
        return new CreateEventCommand(
            "Test Concert",
            "A great test concert",
            DateTime.UtcNow.AddDays(30),
            "Test Venue",
            1000,
            50.00m);
    }

    #endregion
}