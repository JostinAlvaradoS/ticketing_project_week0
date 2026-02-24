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

    [Fact]
    public async Task Handle_WithValidDraftOrder_ShouldUpdateToPendingState()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var userId = "user123";
        var command = new CheckoutOrderCommand(orderId, userId);

        var draftOrder = new Order
        {
            Id = orderId,
            UserId = userId,
            TotalAmount = 150.00m,
            State = "draft",
            CreatedAt = DateTime.UtcNow.AddMinutes(-10),
            Items = new List<OrderItem>
            {
                new OrderItem
                {
                    Id = Guid.NewGuid(),
                    OrderId = orderId,
                    SeatId = Guid.NewGuid(),
                    Price = 75.00m
                },
                new OrderItem
                {
                    Id = Guid.NewGuid(),
                    OrderId = orderId,
                    SeatId = Guid.NewGuid(),
                    Price = 75.00m
                }
            }
        };

        var pendingOrder = new Order
        {
            Id = draftOrder.Id,
            UserId = draftOrder.UserId,
            GuestToken = draftOrder.GuestToken,
            TotalAmount = draftOrder.TotalAmount,
            State = "pending",
            CreatedAt = draftOrder.CreatedAt,
            Items = draftOrder.Items
        };

        _orderRepositoryMock
            .Setup(x => x.GetByIdAsync(orderId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(draftOrder);

        _orderRepositoryMock
            .Setup(x => x.UpdateAsync(It.IsAny<Order>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(pendingOrder);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Success.Should().BeTrue();
        result.ErrorMessage.Should().BeNull();
        result.Order.Should().NotBeNull();
        result.Order!.Id.Should().Be(orderId);
        result.Order.State.Should().Be("pending");
        result.Order.TotalAmount.Should().Be(150.00m);
        result.Order.Items.Should().HaveCount(2);

        _orderRepositoryMock.Verify(x => x.GetByIdAsync(orderId, It.IsAny<CancellationToken>()), Times.Once);
        _orderRepositoryMock.Verify(x => x.UpdateAsync(It.Is<Order>(o => o.State == "pending"), It.IsAny<CancellationToken>()), Times.Once);
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
        var orderId = Guid.NewGuid();
        var command = new CheckoutOrderCommand(orderId, "wronguser");

        var order = new Order
        {
            Id = orderId,
            UserId = "correctuser",
            State = "draft",
            Items = new List<OrderItem> { new OrderItem { Id = Guid.NewGuid() } }
        };

        _orderRepositoryMock
            .Setup(x => x.GetByIdAsync(orderId, It.IsAny<CancellationToken>()))
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
        var orderId = Guid.NewGuid();
        var command = new CheckoutOrderCommand(orderId, null, "wrong-token");

        var order = new Order
        {
            Id = orderId,
            GuestToken = "correct-token",
            State = "draft",
            Items = new List<OrderItem> { new OrderItem { Id = Guid.NewGuid() } }
        };

        _orderRepositoryMock
            .Setup(x => x.GetByIdAsync(orderId, It.IsAny<CancellationToken>()))
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
    [InlineData("fulfilled")]
    [InlineData("cancelled")]
    public async Task Handle_WithNonDraftState_ShouldReturnFailure(string state)
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var userId = "user123";
        var command = new CheckoutOrderCommand(orderId, userId);

        var order = new Order
        {
            Id = orderId,
            UserId = userId,
            State = state,
            Items = new List<OrderItem> { new OrderItem { Id = Guid.NewGuid() } }
        };

        _orderRepositoryMock
            .Setup(x => x.GetByIdAsync(orderId, It.IsAny<CancellationToken>()))
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
        var orderId = Guid.NewGuid();
        var userId = "user123";
        var command = new CheckoutOrderCommand(orderId, userId);

        var emptyOrder = new Order
        {
            Id = orderId,
            UserId = userId,
            State = "draft",
            Items = new List<OrderItem>() // No items
        };

        _orderRepositoryMock
            .Setup(x => x.GetByIdAsync(orderId, It.IsAny<CancellationToken>()))
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
        var orderId = Guid.NewGuid();
        var guestToken = "guest-token-123";
        var command = new CheckoutOrderCommand(orderId, null, guestToken);

        var draftOrder = new Order
        {
            Id = orderId,
            UserId = null,
            GuestToken = guestToken,
            TotalAmount = 100.00m,
            State = "draft",
            CreatedAt = DateTime.UtcNow.AddMinutes(-5),
            Items = new List<OrderItem>
            {
                new OrderItem
                {
                    Id = Guid.NewGuid(),
                    OrderId = orderId,
                    SeatId = Guid.NewGuid(),
                    Price = 100.00m
                }
            }
        };

        var pendingOrder = new Order
        {
            Id = draftOrder.Id,
            UserId = draftOrder.UserId,
            GuestToken = draftOrder.GuestToken,
            TotalAmount = draftOrder.TotalAmount,
            State = "pending",
            CreatedAt = draftOrder.CreatedAt,
            Items = draftOrder.Items
        };

        _orderRepositoryMock
            .Setup(x => x.GetByIdAsync(orderId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(draftOrder);

        _orderRepositoryMock
            .Setup(x => x.UpdateAsync(It.IsAny<Order>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(pendingOrder);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Success.Should().BeTrue();
        result.Order.Should().NotBeNull();
        result.Order!.GuestToken.Should().Be(guestToken);
        result.Order.UserId.Should().BeNull();
        result.Order.State.Should().Be("pending");
    }

    [Fact]
    public async Task Handle_WhenRepositoryThrows_ShouldReturnFailure()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var command = new CheckoutOrderCommand(orderId, "user123");

        _orderRepositoryMock
            .Setup(x => x.GetByIdAsync(orderId, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Database error"));

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("Failed to checkout order");
        result.Order.Should().BeNull();
    }

    [Fact]
    public async Task Handle_WhenUpdateThrows_ShouldReturnFailure()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var userId = "user123";
        var command = new CheckoutOrderCommand(orderId, userId);

        var draftOrder = new Order
        {
            Id = orderId,
            UserId = userId,
            State = "draft",
            Items = new List<OrderItem> { new OrderItem { Id = Guid.NewGuid() } }
        };

        _orderRepositoryMock
            .Setup(x => x.GetByIdAsync(orderId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(draftOrder);

        _orderRepositoryMock
            .Setup(x => x.UpdateAsync(It.IsAny<Order>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Update failed"));

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("Failed to checkout order");
        result.Order.Should().BeNull();
    }
}