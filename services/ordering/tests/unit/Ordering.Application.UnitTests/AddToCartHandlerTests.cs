namespace Ordering.Application.UnitTests.UseCases.AddToCart;

public class AddToCartHandlerTests
{
    private readonly Mock<IOrderRepository> _orderRepositoryMock;
    private readonly Mock<IReservationValidationService> _reservationServiceMock;
    private readonly AddToCartHandler _handler;

    public AddToCartHandlerTests()
    {
        _orderRepositoryMock = new Mock<IOrderRepository>();
        _reservationServiceMock = new Mock<IReservationValidationService>();
        _handler = new AddToCartHandler(_orderRepositoryMock.Object, _reservationServiceMock.Object);
    }

    [Fact]
    public async Task Handle_WithValidReservation_ShouldCreateNewDraftOrder()
    {
        // Arrange
        var command = new AddToCartCommand(
            ReservationId: Guid.NewGuid(),
            SeatId: Guid.NewGuid(),
            Price: 99.99m,
            UserId: "user123");

        _reservationServiceMock
            .Setup(x => x.ValidateReservationAsync(command.ReservationId, command.SeatId))
            .ReturnsAsync(new ReservationValidationResult(true, null, null));

        _orderRepositoryMock
            .Setup(x => x.GetDraftOrderAsync(command.UserId, command.GuestToken, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Order?)null);

        var createdOrder = Order.Create(command.UserId, null);
        createdOrder.AddItem(command.SeatId, command.Price);

        _orderRepositoryMock
            .Setup(x => x.CreateAsync(It.IsAny<Order>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(createdOrder);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Success.Should().BeTrue();
        result.ErrorMessage.Should().BeNull();
        result.Order.Should().NotBeNull();
        result.Order!.UserId.Should().Be(command.UserId);
        result.Order.State.Should().Be(Order.StateDraft);
        result.Order.TotalAmount.Should().Be(command.Price);
        result.Order.Items.Should().HaveCount(1);
        result.Order.Items.First().SeatId.Should().Be(command.SeatId);

        _reservationServiceMock.Verify(x => x.ValidateReservationAsync(command.ReservationId, command.SeatId), Times.Once);
        _orderRepositoryMock.Verify(x => x.CreateAsync(It.IsAny<Order>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WithExistingDraftOrder_ShouldAddItemToExistingOrder()
    {
        // Arrange
        var command = new AddToCartCommand(
            ReservationId: Guid.NewGuid(),
            SeatId: Guid.NewGuid(),
            Price: 75.50m,
            UserId: "user123");

        var existingOrder = Order.Create(command.UserId, null);
        existingOrder.AddItem(Guid.NewGuid(), 50.00m);
        var existingOrderId = existingOrder.Id;

        _reservationServiceMock
            .Setup(x => x.ValidateReservationAsync(command.ReservationId, command.SeatId))
            .ReturnsAsync(new ReservationValidationResult(true, null, null));

        _orderRepositoryMock
            .Setup(x => x.GetDraftOrderAsync(command.UserId, command.GuestToken, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingOrder);

        // Handler mutates existingOrder via AddItem — return the same object
        _orderRepositoryMock
            .Setup(x => x.UpdateAsync(It.IsAny<Order>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingOrder);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Success.Should().BeTrue();
        result.ErrorMessage.Should().BeNull();
        result.Order.Should().NotBeNull();
        result.Order!.Id.Should().Be(existingOrderId);
        result.Order.TotalAmount.Should().Be(125.50m);
        result.Order.Items.Should().HaveCount(2);

        _orderRepositoryMock.Verify(x => x.UpdateAsync(It.IsAny<Order>(), It.IsAny<CancellationToken>()), Times.Once);
        _orderRepositoryMock.Verify(x => x.CreateAsync(It.IsAny<Order>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WithInvalidReservation_ShouldReturnFailure()
    {
        // Arrange
        var command = new AddToCartCommand(
            ReservationId: Guid.NewGuid(),
            SeatId: Guid.NewGuid(),
            Price: 99.99m,
            UserId: "user123");

        _reservationServiceMock
            .Setup(x => x.ValidateReservationAsync(command.ReservationId, command.SeatId))
            .ReturnsAsync(new ReservationValidationResult(false, "Reservation not found", null));

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Be("Reservation not found");
        result.Order.Should().BeNull();

        _reservationServiceMock.Verify(x => x.ValidateReservationAsync(command.ReservationId, command.SeatId), Times.Once);
        _orderRepositoryMock.Verify(x => x.GetDraftOrderAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WithSeatAlreadyInCart_ShouldReturnFailure()
    {
        // Arrange
        var seatId = Guid.NewGuid();
        var command = new AddToCartCommand(
            ReservationId: Guid.NewGuid(),
            SeatId: seatId,
            Price: 99.99m,
            UserId: "user123");

        var existingOrder = Order.Create(command.UserId, null);
        existingOrder.AddItem(seatId, 50.00m); // same seat already in cart

        _reservationServiceMock
            .Setup(x => x.ValidateReservationAsync(command.ReservationId, command.SeatId))
            .ReturnsAsync(new ReservationValidationResult(true, null, null));

        _orderRepositoryMock
            .Setup(x => x.GetDraftOrderAsync(command.UserId, command.GuestToken, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingOrder);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Be("Seat is already in the cart");
        result.Order.Should().BeNull();

        _orderRepositoryMock.Verify(x => x.UpdateAsync(It.IsAny<Order>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WithGuestToken_ShouldCreateOrderForGuest()
    {
        // Arrange
        var command = new AddToCartCommand(
            ReservationId: Guid.NewGuid(),
            SeatId: Guid.NewGuid(),
            Price: 99.99m,
            UserId: null,
            GuestToken: "guest-token-123");

        _reservationServiceMock
            .Setup(x => x.ValidateReservationAsync(command.ReservationId, command.SeatId))
            .ReturnsAsync(new ReservationValidationResult(true, null, null));

        _orderRepositoryMock
            .Setup(x => x.GetDraftOrderAsync(command.UserId, command.GuestToken, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Order?)null);

        var createdOrder = Order.Create(null, command.GuestToken);
        createdOrder.AddItem(command.SeatId, command.Price);

        _orderRepositoryMock
            .Setup(x => x.CreateAsync(It.IsAny<Order>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(createdOrder);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Success.Should().BeTrue();
        result.Order.Should().NotBeNull();
        result.Order!.UserId.Should().BeNull();
        result.Order.GuestToken.Should().Be(command.GuestToken);
    }

    [Fact]
    public async Task Handle_WhenRepositoryThrows_ShouldPropagateException()
    {
        // Arrange
        var command = new AddToCartCommand(
            ReservationId: Guid.NewGuid(),
            SeatId: Guid.NewGuid(),
            Price: 99.99m,
            UserId: "user123");

        _reservationServiceMock
            .Setup(x => x.ValidateReservationAsync(command.ReservationId, command.SeatId))
            .ReturnsAsync(new ReservationValidationResult(true, null, null));

        _orderRepositoryMock
            .Setup(x => x.GetDraftOrderAsync(command.UserId, command.GuestToken, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Database error"));

        // Act & Assert — infrastructure exceptions propagate, they are not swallowed
        await _handler.Invoking(h => h.Handle(command, CancellationToken.None))
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Database error*");
    }

    [Fact]
    public async Task Handle_WithValidationFailure_ShouldReturnFailure()
    {
        // Arrange
        var command = new AddToCartCommand(Guid.NewGuid(), Guid.NewGuid(), 10.0m, "user-1", null);
        var validationResult = new ReservationValidationResult(false, "Seat already reserved", null);

        _reservationServiceMock
            .Setup(x => x.ValidateReservationAsync(command.ReservationId, command.SeatId))
            .ReturnsAsync(validationResult);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Be("Seat already reserved");
        _orderRepositoryMock.Verify(x => x.GetDraftOrderAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
