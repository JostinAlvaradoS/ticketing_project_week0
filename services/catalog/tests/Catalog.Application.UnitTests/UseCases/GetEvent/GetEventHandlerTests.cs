using Catalog.Application.Ports;
using Catalog.Application.UseCases.GetEvent;
using Catalog.Domain.Entities;
using FluentAssertions;
using Moq;

namespace Catalog.Application.UnitTests.UseCases.GetEvent;

public class GetEventHandlerTests
{
    private readonly Mock<ICatalogRepository> _mockRepository;
    private readonly GetEventHandler _handler;

    public GetEventHandlerTests()
    {
        _mockRepository = new Mock<ICatalogRepository>();
        _handler = new GetEventHandler(_mockRepository.Object);
    }

    [Fact]
    public async Task Handle_WithExistingEvent_ShouldReturnEventResponse()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var cancellationToken = CancellationToken.None;
        var query = new GetEventQuery(eventId);

        var eventEntity = CreateEvent(eventId, "Test Concert", "Great concert", DateTime.UtcNow.AddDays(15), 125.50m);

        _mockRepository
            .Setup(r => r.GetEventAsync(eventId, cancellationToken))
            .ReturnsAsync(eventEntity);

        // Act
        var result = await _handler.Handle(query, cancellationToken);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(eventEntity.Id);
        result.Name.Should().Be(eventEntity.Name);
        result.Description.Should().Be(eventEntity.Description);
        result.EventDate.Should().Be(eventEntity.EventDate);
        result.BasePrice.Should().Be(eventEntity.BasePrice);

        _mockRepository.Verify(r => r.GetEventAsync(eventId, cancellationToken), Times.Once);
    }

    [Fact]
    public async Task Handle_WithNonExistentEvent_ShouldReturnNull()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var cancellationToken = CancellationToken.None;
        var query = new GetEventQuery(eventId);

        _mockRepository
            .Setup(r => r.GetEventAsync(eventId, cancellationToken))
            .ReturnsAsync((Event?)null);

        // Act
        var result = await _handler.Handle(query, cancellationToken);

        // Assert
        result.Should().BeNull();
        _mockRepository.Verify(r => r.GetEventAsync(eventId, cancellationToken), Times.Once);
    }

    [Fact]
    public async Task Handle_WithRepositoryException_ShouldPropagateException()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var cancellationToken = CancellationToken.None;
        var query = new GetEventQuery(eventId);

        var expectedException = new InvalidOperationException("Database connection failed");
        _mockRepository
            .Setup(r => r.GetEventAsync(eventId, cancellationToken))
            .ThrowsAsync(expectedException);

        // Act & Assert
        var action = async () => await _handler.Handle(query, cancellationToken);
        await action.Should().ThrowAsync<InvalidOperationException>()
                   .WithMessage("Database connection failed");

        _mockRepository.Verify(r => r.GetEventAsync(eventId, cancellationToken), Times.Once);
    }

    [Fact]
    public async Task Handle_WithCancellationToken_ShouldPassTokenToRepository()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        using var cancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = cancellationTokenSource.Token;
        var query = new GetEventQuery(eventId);

        _mockRepository
            .Setup(r => r.GetEventAsync(eventId, cancellationToken))
            .ReturnsAsync((Event?)null);

        // Act
        await _handler.Handle(query, cancellationToken);

        // Assert
        _mockRepository.Verify(r => r.GetEventAsync(eventId, cancellationToken), Times.Once);
    }

    [Fact]
    public async Task Handle_WithValidEventId_ShouldCallRepositoryWithCorrectId()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var cancellationToken = CancellationToken.None;
        var query = new GetEventQuery(eventId);

        _mockRepository
            .Setup(r => r.GetEventAsync(eventId, cancellationToken))
            .ReturnsAsync((Event?)null);

        // Act
        await _handler.Handle(query, cancellationToken);

        // Assert
        _mockRepository.Verify(r => r.GetEventAsync(eventId, cancellationToken), Times.Once);
        _mockRepository.Verify(r => r.GetEventAsync(It.Is<Guid>(id => id == eventId), cancellationToken), Times.Once);
    }

    [Fact]
    public async Task Handle_WithEventHavingSpecialCharacters_ShouldHandleCorrectly()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var cancellationToken = CancellationToken.None;
        var query = new GetEventQuery(eventId);

        var eventEntity = CreateEvent(
            eventId, 
            "Música & Café: João's Concert", 
            "Special characters: áéíóú ñ ü @#$%", 
            DateTime.UtcNow.AddDays(10), 
            99.99m
        );

        _mockRepository
            .Setup(r => r.GetEventAsync(eventId, cancellationToken))
            .ReturnsAsync(eventEntity);

        // Act
        var result = await _handler.Handle(query, cancellationToken);

        // Assert
        result.Should().NotBeNull();
        result!.Name.Should().Be("Música & Café: João's Concert");
        result.Description.Should().Be("Special characters: áéíóú ñ ü @#$%");
    }

    [Fact] 
    public async Task Handle_WithEventHavingMinimalValidData_ShouldReturnCorrectResponse()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var cancellationToken = CancellationToken.None;
        var query = new GetEventQuery(eventId);

        var eventEntity = CreateEvent(eventId, "E", "Valid description", DateTime.UtcNow.AddSeconds(1), 0.01m);

        _mockRepository
            .Setup(r => r.GetEventAsync(eventId, cancellationToken))
            .ReturnsAsync(eventEntity);

        // Act
        var result = await _handler.Handle(query, cancellationToken);

        // Assert
        result.Should().NotBeNull();
        result!.Name.Should().Be("E");
        result.Description.Should().Be("Valid description");
        result.BasePrice.Should().Be(0.01m);
    }

    // Test Helper Methods
    private static Event CreateEvent(Guid id, string name, string description, DateTime eventDate, decimal basePrice)
    {
        var eventEntity = Event.Create(
            name,
            description,
            eventDate,
            "Test Venue",
            1000,
            basePrice);
        
        // Use reflection to set the ID for testing purposes
        typeof(Event).GetProperty("Id")?.SetValue(eventEntity, id);
        
        return eventEntity;
    }
}