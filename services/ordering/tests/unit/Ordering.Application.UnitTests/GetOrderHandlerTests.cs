using FluentAssertions;
using Moq;
using Ordering.Application.DTOs;
using Ordering.Application.Ports;
using Ordering.Application.UseCases.GetOrder;
using Ordering.Domain.Entities;
using Xunit;

namespace Ordering.Application.UnitTests.UseCases.GetOrder;

public class GetOrderHandlerTests
{
    private readonly Mock<IOrderRepository> _orderRepositoryMock;
    private readonly GetOrderHandler _handler;

    public GetOrderHandlerTests()
    {
        _orderRepositoryMock = new Mock<IOrderRepository>();
        _handler = new GetOrderHandler(_orderRepositoryMock.Object);
    }

    [Fact]
    public async Task Handle_WithExistingOrder_ShouldReturnOrderDto()
    {
        // Arrange
        var order = Order.Create("user123", null);
        var orderId = order.Id;
        _orderRepositoryMock.Setup(r => r.GetByIdAsync(orderId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);

        // Act
        var result = await _handler.Handle(new GetOrderQuery(orderId), CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(order.Id);
    }

    [Fact]
    public async Task Handle_WithNonExistingOrder_ShouldReturnNull()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        _orderRepositoryMock.Setup(r => r.GetByIdAsync(orderId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Order?)null);

        // Act
        var result = await _handler.Handle(new GetOrderQuery(orderId), CancellationToken.None);

        // Assert
        result.Should().BeNull();
    }
}
