using Catalog.Application.Ports;
using Catalog.Application.UseCases.GenerateSeats;
using Catalog.Domain.Entities;
using FluentAssertions;
using Moq;

namespace Catalog.Application.UnitTests.UseCases.GenerateSeats;

public class GenerateSeatsCommandHandlerTests
{
    private readonly Mock<ICatalogRepository> _mockRepository;
    private readonly Mock<IKafkaProducer> _mockKafkaProducer;
    private readonly GenerateSeatsCommandHandler _handler;

    public GenerateSeatsCommandHandlerTests()
    {
        _mockRepository = new Mock<ICatalogRepository>();
        _mockKafkaProducer = new Mock<IKafkaProducer>();
        _handler = new GenerateSeatsCommandHandler(_mockRepository.Object, _mockKafkaProducer.Object);
    }

    #region Generate Seats Tests - Following Gherkin Scenarios (T103)

    [Fact]
    public async Task Handle_WithValidConfiguration_ShouldGenerateSeatsCorrectly()
    {
        // Arrange - Following Gherkin: "Generar asientos masivos para un evento"
        var eventId = new Guid("11111111-1111-1111-1111-111111111111");
        var eventEntity = CreateValidEvent(eventId, 100.00m, 5000); // Large capacity

        var command = new GenerateSeatsCommand(
            eventId,
            new List<SeatSectionConfiguration>
            {
                new("A", 50, 20, 1.0m),
                new("B", 50, 20, 1.0m),
                new("C", 50, 20, 1.0m)
            });

        _mockRepository
            .Setup(r => r.GetEventAsync(eventId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(eventEntity);

        var capturedSeats = new List<Seat>();
        _mockRepository
            .Setup(r => r.AddSeatsAsync(It.IsAny<IEnumerable<Seat>>(), It.IsAny<CancellationToken>()))
            .Callback<IEnumerable<Seat>, CancellationToken>((seats, _) => capturedSeats.AddRange(seats));

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert - Following Gherkin: "se crean 3000 asientos en total (3 secciones × 50 filas × 20 asientos)"
        result.Should().NotBeNull();
        result.EventId.Should().Be(eventId);
        result.TotalSeatsGenerated.Should().Be(3000); // 3 sections × 50 rows × 20 seats

        // Verify section summaries
        result.SectionSummaries.Should().HaveCount(3);
        result.SectionSummaries.Should().Contain(s => s.SectionCode == "A" && s.SeatsGenerated == 1000);
        result.SectionSummaries.Should().Contain(s => s.SectionCode == "B" && s.SeatsGenerated == 1000);
        result.SectionSummaries.Should().Contain(s => s.SectionCode == "C" && s.SeatsGenerated == 1000);

        // Verify captured seats - Following Gherkin: "todos los asientos tienen estado 'available'"
        capturedSeats.Should().HaveCount(3000);
        capturedSeats.Should().OnlyContain(s => s.Status == Seat.StatusAvailable);

        // Verify seat numbering - Following Gherkin: "los asientos están numerados correctamente"
        var sectionASeats = capturedSeats.Where(s => s.SectionCode == "A").ToList();
        sectionASeats.Should().HaveCount(1000);
        sectionASeats.Should().Contain(s => s.RowNumber == 1 && s.SeatNumber == 1);
        sectionASeats.Should().Contain(s => s.RowNumber == 50 && s.SeatNumber == 20);
        sectionASeats.Max(s => s.RowNumber).Should().Be(50);
        sectionASeats.Max(s => s.SeatNumber).Should().Be(20);

        // Verify repository interactions
        _mockRepository.Verify(r => r.GetEventAsync(eventId, It.IsAny<CancellationToken>()), Times.Once);
        _mockRepository.Verify(r => r.AddSeatsAsync(It.IsAny<IEnumerable<Seat>>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockRepository.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WithDifferentPriceMultipliers_ShouldCalculateCorrectPrices()
    {
        // Arrange - Following Gherkin: "Generar asientos con diferentes precios por sección"
        var eventId = Guid.NewGuid();
        var basePrice = 100.00m;
        var eventEntity = CreateValidEvent(eventId, basePrice, 1000);

        var command = new GenerateSeatsCommand(
            eventId,
            new List<SeatSectionConfiguration>
            {
                new("VIP", 5, 10, 2.0m),     // 200.00 price
                new("GOLD", 10, 15, 1.5m),   // 150.00 price  
                new("REGULAR", 20, 25, 1.0m) // 100.00 price
            });

        _mockRepository
            .Setup(r => r.GetEventAsync(eventId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(eventEntity);

        var capturedSeats = new List<Seat>();
        _mockRepository
            .Setup(r => r.AddSeatsAsync(It.IsAny<IEnumerable<Seat>>(), It.IsAny<CancellationToken>()))
            .Callback<IEnumerable<Seat>, CancellationToken>((seats, _) => capturedSeats.AddRange(seats));

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert - Following Gherkin seat counts and prices
        result.Should().NotBeNull();
        result.TotalSeatsGenerated.Should().Be(700); // 50 + 150 + 500

        // Verify section summaries match Gherkin expectations
        var vipSummary = result.SectionSummaries.First(s => s.SectionCode == "VIP");
        vipSummary.SeatsGenerated.Should().Be(50); // 5 × 10
        vipSummary.SeatPrice.Should().Be(200.00m); // 100.00 × 2.0

        var goldSummary = result.SectionSummaries.First(s => s.SectionCode == "GOLD");
        goldSummary.SeatsGenerated.Should().Be(150); // 10 × 15
        goldSummary.SeatPrice.Should().Be(150.00m); // 100.00 × 1.5

        var regularSummary = result.SectionSummaries.First(s => s.SectionCode == "REGULAR");
        regularSummary.SeatsGenerated.Should().Be(500); // 20 × 25
        regularSummary.SeatPrice.Should().Be(100.00m); // 100.00 × 1.0

        // Verify actual seat prices in captured seats
        var vipSeats = capturedSeats.Where(s => s.SectionCode == "VIP").ToList();
        vipSeats.Should().OnlyContain(s => s.Price == 200.00m);

        var goldSeats = capturedSeats.Where(s => s.SectionCode == "GOLD").ToList();
        goldSeats.Should().OnlyContain(s => s.Price == 150.00m);

        var regularSeats = capturedSeats.Where(s => s.SectionCode == "REGULAR").ToList();
        regularSeats.Should().OnlyContain(s => s.Price == 100.00m);
    }

    [Fact]
    public async Task Handle_WithNonExistentEvent_ShouldThrowError()
    {
        // Arrange - Following Gherkin: "Error al generar asientos para evento inexistente"
        var nonExistentEventId = new Guid("99999999-9999-9999-9999-999999999999");
        var command = new GenerateSeatsCommand(
            nonExistentEventId,
            new List<SeatSectionConfiguration>
            {
                new("A", 10, 10, 1.0m)
            });

        _mockRepository
            .Setup(r => r.GetEventAsync(nonExistentEventId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Event?)null); // Event not found

        // Act & Assert - Following Gherkin: "recibo un error 'Evento no encontrado'"
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _handler.Handle(command, CancellationToken.None));
        
        exception.Message.Should().Contain("Evento no encontrado");

        // Verify NO seats are created - Following Gherkin: "NO se crean asientos"
        _mockRepository.Verify(r => r.AddSeatsAsync(It.IsAny<IEnumerable<Seat>>(), It.IsAny<CancellationToken>()), Times.Never);
        _mockRepository.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WithExcessiveSeats_ShouldThrowCapacityError()
    {
        // Arrange - Following Gherkin: "Validar capacidad máxima vs asientos generados"
        var eventId = Guid.NewGuid();
        var eventEntity = CreateValidEvent(eventId, 50.00m, 100); // Small capacity

        var command = new GenerateSeatsCommand(
            eventId,
            new List<SeatSectionConfiguration>
            {
                new("A", 10, 15, 1.0m) // Would create 150 seats, exceeds capacity of 100
            });

        _mockRepository
            .Setup(r => r.GetEventAsync(eventId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(eventEntity);

        // Act & Assert - Following Gherkin: "La cantidad de asientos excede la capacidad máxima del evento"
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _handler.Handle(command, CancellationToken.None));
        
        exception.Message.Should().Contain("La cantidad de asientos excede la capacidad máxima del evento");

        // Verify NO seats are persisted when validation fails
        _mockRepository.Verify(r => r.AddSeatsAsync(It.IsAny<IEnumerable<Seat>>(), It.IsAny<CancellationToken>()), Times.Never);
        _mockRepository.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ShouldCallRepositoryInCorrectOrder()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var eventEntity = CreateValidEvent(eventId, 75.00m, 1000);
        
        var command = new GenerateSeatsCommand(
            eventId,
            new List<SeatSectionConfiguration> { new("A", 5, 5, 1.0m) });

        _mockRepository
            .Setup(r => r.GetEventAsync(eventId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(eventEntity);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert - Verify the sequence of calls
        var sequence = new MockSequence();
        _mockRepository.InSequence(sequence)
            .Setup(r => r.GetEventAsync(eventId, It.IsAny<CancellationToken>()));
        _mockRepository.InSequence(sequence)
            .Setup(r => r.AddSeatsAsync(It.IsAny<IEnumerable<Seat>>(), It.IsAny<CancellationToken>()));
        _mockRepository.InSequence(sequence)
            .Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()));
    }

    [Fact]
    public async Task Handle_WhenAddSeatsThrows_ShouldPropagateException()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var eventEntity = CreateValidEvent(eventId, 50.00m, 1000);
        var command = new GenerateSeatsCommand(
            eventId,
            new List<SeatSectionConfiguration> { new("A", 2, 2, 1.0m) });

        var databaseException = new InvalidOperationException("Database connection failed");

        _mockRepository
            .Setup(r => r.GetEventAsync(eventId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(eventEntity);
        
        _mockRepository
            .Setup(r => r.AddSeatsAsync(It.IsAny<IEnumerable<Seat>>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(databaseException);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _handler.Handle(command, CancellationToken.None));
        
        exception.Should().Be(databaseException);

        // SaveChanges should not be called if AddSeatsAsync fails
        _mockRepository.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    #endregion

    #region Test Helper Methods

    private static Event CreateValidEvent(Guid eventId, decimal basePrice, int maxCapacity)
    {
        var eventEntity = Event.Create(
            "Test Event",
            "Test Description",
            DateTime.UtcNow.AddDays(30),
            "Test Venue",
            maxCapacity,
            basePrice);
        
        // Use reflection to set ID for testing
        typeof(Event).GetProperty("Id")?.SetValue(eventEntity, eventId);
        
        return eventEntity;
    }

    #endregion
}