using System.Text.Json;
using FluentAssertions;
using Inventory.Application.UseCases.CreateReservation;
using Inventory.Domain.Entities;
using Inventory.Domain.Ports;
using Inventory.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace Inventory.UnitTests.Application;

public class CreateReservationCommandHandlerTests
{
    private readonly InventoryDbContext _context;
    private readonly Mock<IRedisLock> _redisLockMock;
    private readonly Mock<IKafkaProducer> _kafkaProducerMock;
    private readonly CreateReservationCommandHandler _handler;

    public CreateReservationCommandHandlerTests()
    {
        var options = new DbContextOptionsBuilder<InventoryDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        
        _context = new InventoryDbContext(options);
        _redisLockMock = new Mock<IRedisLock>();
        _kafkaProducerMock = new Mock<IKafkaProducer>();
        
        _handler = new CreateReservationCommandHandler(
            _context,
            _redisLockMock.Object,
            _kafkaProducerMock.Object);
    }

    [Fact]
    public async Task Handle_WithAvailableSeat_ShouldCreateReservation()
    {
        // Arrange
        var seatId = Guid.NewGuid();
        var seat = new Seat { Id = seatId, Section = "A", Row = "1", Number = 10, Reserved = false };
        _context.Seats.Add(seat);
        await _context.SaveChangesAsync();

        var command = new CreateReservationCommand(seatId, "customer-123");
        
        _redisLockMock.Setup(l => l.AcquireLockAsync(It.IsAny<string>(), It.IsAny<TimeSpan>()))
            .ReturnsAsync("valid-lock-token");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Status.Should().Be("active");
        
        var savedReservation = await _context.Reservations.FirstOrDefaultAsync(r => r.SeatId == seatId);
        savedReservation.Should().NotBeNull();
        savedReservation!.CustomerId.Should().Be("customer-123");
        
        var updatedSeat = await _context.Seats.FindAsync(seatId);
        updatedSeat!.Reserved.Should().BeTrue();
        
        _kafkaProducerMock.Verify(p => p.ProduceAsync("reservation-created", It.IsAny<string>(), seatId.ToString("N")), Times.Once);
    }

    [Fact]
    public async Task Handle_WithAlreadyReservedSeat_ShouldThrowException()
    {
        // Arrange
        var seatId = Guid.NewGuid();
        var seat = new Seat { Id = seatId, Section = "A", Row = "1", Number = 10, Reserved = true };
        _context.Seats.Add(seat);
        await _context.SaveChangesAsync();

        var command = new CreateReservationCommand(seatId, "customer-123");
        
        _redisLockMock.Setup(l => l.AcquireLockAsync(It.IsAny<string>(), It.IsAny<TimeSpan>()))
            .ReturnsAsync("valid-lock-token");

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => _handler.Handle(command, CancellationToken.None));
    }
}
