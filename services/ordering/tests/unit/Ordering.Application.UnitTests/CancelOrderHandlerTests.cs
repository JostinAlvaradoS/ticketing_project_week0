namespace Ordering.Application.UnitTests.UseCases.CancelOrder;

public class CancelOrderHandlerTests
{
    private readonly Mock<IOrderRepository> _orderRepositoryMock;
    private readonly CancelOrderHandler _handler;

    public CancelOrderHandlerTests()
    {
        _orderRepositoryMock = new Mock<IOrderRepository>();
        _handler = new CancelOrderHandler(_orderRepositoryMock.Object);
    }

    [Fact]
    public async Task Handle_ExistingCancellableOrder_ReturnsTrueAndPersists()
    {
        // Arrange — orden en Draft (cancelable)
        var order = Order.Create("user-1", null);
        var command = new CancelOrderCommand(order.Id);

        _orderRepositoryMock
            .Setup(r => r.GetByIdAsync(order.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);
        _orderRepositoryMock
            .Setup(r => r.UpdateAsync(order, It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().BeTrue();
        order.State.Should().Be(Order.StateCancelled);
        _orderRepositoryMock.Verify(r => r.UpdateAsync(order, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_OrderNotFound_ReturnsFalseWithoutUpdate()
    {
        // Arrange
        var command = new CancelOrderCommand(Guid.NewGuid());

        _orderRepositoryMock
            .Setup(r => r.GetByIdAsync(command.OrderId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Order?)null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().BeFalse();
        _orderRepositoryMock.Verify(r => r.UpdateAsync(It.IsAny<Order>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_OrderInPaidState_ReturnsFalseWithoutUpdate()
    {
        // Arrange — orden pagada no puede cancelarse (guard en el dominio)
        var order = Order.Create("user-1", null);
        order.AddItem(Guid.NewGuid(), 100m);
        order.Checkout();
        order.MarkAsPaid();
        var command = new CancelOrderCommand(order.Id);

        _orderRepositoryMock
            .Setup(r => r.GetByIdAsync(order.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert — InvalidOperationException capturada internamente → false
        result.Should().BeFalse();
        _orderRepositoryMock.Verify(r => r.UpdateAsync(It.IsAny<Order>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
