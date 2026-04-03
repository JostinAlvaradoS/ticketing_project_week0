namespace Ordering.Application.UnitTests.UseCases.CheckoutOrder;

public class CheckoutOrderHandlerTests
{
    private readonly Mock<IOrderRepository> _orderRepositoryMock;
    private readonly CheckoutOrderHandler _handler;

    public CheckoutOrderHandlerTests()
    {
        _orderRepositoryMock = new Mock<IOrderRepository>();
        _handler = new CheckoutOrderHandler(_orderRepositoryMock.Object);
    }

    // Helper: creates an Order and advances it to the target state using domain methods
    private static Order CreateOrderInState(string userId, string targetState)
    {
        var order = Order.Create(userId, null);
        switch (targetState)
        {
            case Order.StatePending:
                order.AddItem(Guid.NewGuid(), 10.00m);
                order.Checkout();
                break;
            case Order.StatePaid:
                order.AddItem(Guid.NewGuid(), 10.00m);
                order.Checkout();
                order.MarkAsPaid();
                break;
            case Order.StateCancelled:
                order.Cancel();
                break;
        }
        return order;
    }

    [Fact]
    public async Task Handle_WithValidDraftOrder_ShouldUpdateToPendingState()
    {
        // Arrange
        var userId = "user123";

        var draftOrder = Order.Create(userId, null);
        draftOrder.AddItem(Guid.NewGuid(), 75.00m);
        draftOrder.AddItem(Guid.NewGuid(), 75.00m);
        var orderId = draftOrder.Id;

        var command = new CheckoutOrderCommand(orderId, userId);

        _orderRepositoryMock
            .Setup(x => x.GetByIdAsync(orderId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(draftOrder);

        // Handler calls Checkout() on draftOrder (mutates it to "pending"), then UpdateAsync
        _orderRepositoryMock
            .Setup(x => x.UpdateAsync(It.IsAny<Order>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(draftOrder);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Success.Should().BeTrue();
        result.ErrorMessage.Should().BeNull();
        result.Order.Should().NotBeNull();
        result.Order!.Id.Should().Be(orderId);
        result.Order.State.Should().Be(Order.StatePending);
        result.Order.TotalAmount.Should().Be(150.00m);
        result.Order.Items.Should().HaveCount(2);

        _orderRepositoryMock.Verify(x => x.GetByIdAsync(orderId, It.IsAny<CancellationToken>()), Times.Once);
        _orderRepositoryMock.Verify(x => x.UpdateAsync(It.Is<Order>(o => o.State == Order.StatePending), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WithNonExistentOrder_ShouldReturnFailure()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var command = new CheckoutOrderCommand(orderId, "user123");

        _orderRepositoryMock
            .Setup(x => x.GetByIdAsync(orderId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Order?)null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Be("Order not found");
        result.Order.Should().BeNull();

        _orderRepositoryMock.Verify(x => x.UpdateAsync(It.IsAny<Order>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WithWrongUserId_ShouldReturnUnauthorized()
    {
        // Arrange
        var order = Order.Create("correctuser", null);
        var command = new CheckoutOrderCommand(order.Id, "wronguser");

        _orderRepositoryMock
            .Setup(x => x.GetByIdAsync(order.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Be("Unauthorized");
        result.Order.Should().BeNull();

        _orderRepositoryMock.Verify(x => x.UpdateAsync(It.IsAny<Order>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WithWrongGuestToken_ShouldReturnUnauthorized()
    {
        // Arrange
        var order = Order.Create(null, "correct-token");
        var command = new CheckoutOrderCommand(order.Id, null, "wrong-token");

        _orderRepositoryMock
            .Setup(x => x.GetByIdAsync(order.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Be("Unauthorized");
        result.Order.Should().BeNull();

        _orderRepositoryMock.Verify(x => x.UpdateAsync(It.IsAny<Order>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Theory]
    [InlineData("pending")]
    [InlineData("paid")]
    [InlineData("cancelled")]
    public async Task Handle_WithNonDraftState_ShouldReturnFailure(string state)
    {
        // Arrange
        var userId = "user123";
        var order = CreateOrderInState(userId, state);
        var command = new CheckoutOrderCommand(order.Id, userId);

        _orderRepositoryMock
            .Setup(x => x.GetByIdAsync(order.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Be("Order is not in draft state");
        result.Order.Should().BeNull();

        _orderRepositoryMock.Verify(x => x.UpdateAsync(It.IsAny<Order>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WithEmptyOrder_ShouldReturnFailure()
    {
        // Arrange
        var userId = "user123";
        var emptyOrder = Order.Create(userId, null); // no items added
        var command = new CheckoutOrderCommand(emptyOrder.Id, userId);

        _orderRepositoryMock
            .Setup(x => x.GetByIdAsync(emptyOrder.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(emptyOrder);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Be("Order is empty");
        result.Order.Should().BeNull();

        _orderRepositoryMock.Verify(x => x.UpdateAsync(It.IsAny<Order>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WithGuestToken_ShouldSuccessfullyCheckout()
    {
        // Arrange
        var guestToken = "guest-token-123";

        var draftOrder = Order.Create(null, guestToken);
        draftOrder.AddItem(Guid.NewGuid(), 100.00m);
        var orderId = draftOrder.Id;

        var command = new CheckoutOrderCommand(orderId, null, guestToken);

        _orderRepositoryMock
            .Setup(x => x.GetByIdAsync(orderId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(draftOrder);

        _orderRepositoryMock
            .Setup(x => x.UpdateAsync(It.IsAny<Order>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(draftOrder);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Success.Should().BeTrue();
        result.Order.Should().NotBeNull();
        result.Order!.GuestToken.Should().Be(guestToken);
        result.Order.UserId.Should().BeNull();
        result.Order.State.Should().Be(Order.StatePending);
    }

    [Fact]
    public async Task Handle_WhenRepositoryThrows_ShouldPropagateException()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var command = new CheckoutOrderCommand(orderId, "user123");

        _orderRepositoryMock
            .Setup(x => x.GetByIdAsync(orderId, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Database error"));

        // Act & Assert — infrastructure exceptions propagate, they are not swallowed
        await _handler.Invoking(h => h.Handle(command, CancellationToken.None))
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Database error*");
    }

    [Fact]
    public async Task Handle_WhenUpdateThrows_ShouldPropagateException()
    {
        // Arrange
        var userId = "user123";

        var draftOrder = Order.Create(userId, null);
        draftOrder.AddItem(Guid.NewGuid(), 50.00m);
        var command = new CheckoutOrderCommand(draftOrder.Id, userId);

        _orderRepositoryMock
            .Setup(x => x.GetByIdAsync(draftOrder.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(draftOrder);

        _orderRepositoryMock
            .Setup(x => x.UpdateAsync(It.IsAny<Order>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Update failed"));

        // Act & Assert — infrastructure exceptions propagate, they are not swallowed
        await _handler.Invoking(h => h.Handle(command, CancellationToken.None))
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Update failed*");
    }

    [Fact]
    public async Task Handle_WithUnauthorizedUser_ShouldReturnFailure()
    {
        // Arrange
        var order = Order.Create("correct-user", null);
        var command = new CheckoutOrderCommand(order.Id, "wrong-user");

        _orderRepositoryMock
            .Setup(x => x.GetByIdAsync(order.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Be("Unauthorized");
    }
}
