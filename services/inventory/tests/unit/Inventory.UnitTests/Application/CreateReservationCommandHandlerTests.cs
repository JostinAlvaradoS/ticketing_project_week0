using FluentAssertions;
using Inventory.Application.Ports;
using Inventory.Application.UseCases.CreateReservation;
using Inventory.Domain.Entities;
using Moq;

namespace Inventory.UnitTests.Application;

public class CreateReservationCommandHandlerTests
{
    private readonly Mock<ISeatRepository> _seatRepositoryMock;
    private readonly Mock<IReservationRepository> _reservationRepositoryMock;
    private readonly Mock<IRedisLock> _redisLockMock;
    private readonly Mock<IKafkaProducer> _kafkaProducerMock;
    private readonly CreateReservationCommandHandler _handler;

    public CreateReservationCommandHandlerTests()
    {
        _seatRepositoryMock = new Mock<ISeatRepository>();
        _reservationRepositoryMock = new Mock<IReservationRepository>();
        _redisLockMock = new Mock<IRedisLock>();
        _kafkaProducerMock = new Mock<IKafkaProducer>();

        _handler = new CreateReservationCommandHandler(
            _seatRepositoryMock.Object,
            _reservationRepositoryMock.Object,
            _redisLockMock.Object,
            _kafkaProducerMock.Object);
    }

    [Fact]
    public async Task Handle_WithAvailableSeat_ShouldCreateReservation()
    {
        // Arrange
        var seatId = Guid.NewGuid();
        var seat = new Seat { Id = seatId, Section = "A", Row = "1", Number = 10 }; // Reserved defaults to false

        _redisLockMock
            .Setup(l => l.AcquireLockAsync(It.IsAny<string>(), It.IsAny<TimeSpan>()))
            .ReturnsAsync("valid-lock-token");

        _seatRepositoryMock
            .Setup(r => r.GetByIdAsync(seatId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(seat);

        _reservationRepositoryMock
            .Setup(r => r.CreateAsync(It.IsAny<Reservation>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Reservation r, CancellationToken _) => r);

        var command = new CreateReservationCommand(seatId, "customer-123");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.SeatId.Should().Be(seatId);
        result.CustomerId.Should().Be("customer-123");
        result.Status.Should().Be(Reservation.StatusActive);

        _seatRepositoryMock.Verify(r => r.UpdateAsync(It.Is<Seat>(s => s.Reserved), It.IsAny<CancellationToken>()), Times.Once);
        _reservationRepositoryMock.Verify(r => r.CreateAsync(It.IsAny<Reservation>(), It.IsAny<CancellationToken>()), Times.Once);
        _kafkaProducerMock.Verify(p => p.ProduceAsync("reservation-created", It.IsAny<string>(), seatId.ToString("N")), Times.Once);
    }

    [Fact]
    public async Task Handle_WithAlreadyReservedSeat_ShouldThrowException()
    {
        // Arrange
        var seatId = Guid.NewGuid();
        var seat = new Seat { Id = seatId, Section = "A", Row = "1", Number = 10 };
        seat.Reserve(); // seat already reserved via domain method

        _redisLockMock
            .Setup(l => l.AcquireLockAsync(It.IsAny<string>(), It.IsAny<TimeSpan>()))
            .ReturnsAsync("valid-lock-token");

        _seatRepositoryMock
            .Setup(r => r.GetByIdAsync(seatId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(seat);

        var command = new CreateReservationCommand(seatId, "customer-123");

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => _handler.Handle(command, CancellationToken.None));

        _reservationRepositoryMock.Verify(r => r.CreateAsync(It.IsAny<Reservation>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WithLockAcquisitionFailure_ShouldThrowException()
    {
        // Arrange
        var command = new CreateReservationCommand(Guid.NewGuid(), "customer-123");

        _redisLockMock
            .Setup(l => l.AcquireLockAsync(It.IsAny<string>(), It.IsAny<TimeSpan>()))
            .ReturnsAsync((string?)null); // lock not acquired

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => _handler.Handle(command, CancellationToken.None));

        _seatRepositoryMock.Verify(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WithNonExistentSeat_ShouldThrowException()
    {
        // Arrange
        var seatId = Guid.NewGuid();
        var command = new CreateReservationCommand(seatId, "customer-123");

        _redisLockMock
            .Setup(l => l.AcquireLockAsync(It.IsAny<string>(), It.IsAny<TimeSpan>()))
            .ReturnsAsync("valid-lock-token");

        _seatRepositoryMock
            .Setup(r => r.GetByIdAsync(seatId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Seat?)null);

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(() => _handler.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_WithEmptySeatId_ShouldThrowArgumentException()
    {
        // Arrange
        var command = new CreateReservationCommand(Guid.Empty, "customer-123");

        // Act & Assert — guard clause antes de cualquier I/O
        await Assert.ThrowsAsync<ArgumentException>(() => _handler.Handle(command, CancellationToken.None));

        _redisLockMock.Verify(l => l.AcquireLockAsync(It.IsAny<string>(), It.IsAny<TimeSpan>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WithEmptyCustomerId_ShouldThrowArgumentException()
    {
        // Arrange
        var command = new CreateReservationCommand(Guid.NewGuid(), "");

        // Act & Assert — guard clause antes de cualquier I/O
        await Assert.ThrowsAsync<ArgumentException>(() => _handler.Handle(command, CancellationToken.None));

        _redisLockMock.Verify(l => l.AcquireLockAsync(It.IsAny<string>(), It.IsAny<TimeSpan>()), Times.Never);
    }
}
