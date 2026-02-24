namespace Ordering.Domain.UnitTests;

public class OrderItemTests
{
    [Fact]
    public void OrderItem_ShouldBeCreated_WithCorrectProperties()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var seatId = Guid.NewGuid();
        var price = 99.99m;

        // Act
        var orderItem = new OrderItem
        {
            Id = Guid.NewGuid(),
            OrderId = orderId,
            SeatId = seatId,
            Price = price
        };

        // Assert
        orderItem.Id.Should().NotBeEmpty();
        orderItem.OrderId.Should().Be(orderId);
        orderItem.SeatId.Should().Be(seatId);
        orderItem.Price.Should().Be(price);
        orderItem.Order.Should().BeNull(); // Navigation property not set
    }

    [Fact]
    public void OrderItem_ShouldHaveUniqueId_WhenCreated()
    {
        // Arrange & Act
        var item1 = new OrderItem { Id = Guid.NewGuid() };
        var item2 = new OrderItem { Id = Guid.NewGuid() };

        // Assert
        item1.Id.Should().NotBe(item2.Id);
        item1.Id.Should().NotBeEmpty();
        item2.Id.Should().NotBeEmpty();
    }

    [Fact]
    public void OrderItem_ShouldAcceptZeroPrice()
    {
        // Arrange
        var orderItem = new OrderItem();

        // Act
        orderItem.Price = 0;

        // Assert
        orderItem.Price.Should().Be(0);
    }

    [Fact]
    public void OrderItem_ShouldAcceptPositivePrice()
    {
        // Arrange
        var orderItem = new OrderItem();
        var price = 150.75m;

        // Act
        orderItem.Price = price;

        // Assert
        orderItem.Price.Should().Be(price);
    }

    [Fact]
    public void OrderItem_ShouldSetNavigationProperty_WhenOrderAssigned()
    {
        // Arrange
        var order = new Order { Id = Guid.NewGuid() };
        var orderItem = new OrderItem { Id = Guid.NewGuid(), OrderId = order.Id };

        // Act
        orderItem.Order = order;

        // Assert
        orderItem.Order.Should().Be(order);
        orderItem.OrderId.Should().Be(order.Id);
    }

    [Fact]
    public void OrderItem_ShouldBelongToOrder_WhenAddedToCollection()
    {
        // Arrange
        var order = new Order { Id = Guid.NewGuid() };
        var orderItem = new OrderItem 
        { 
            Id = Guid.NewGuid(), 
            OrderId = order.Id,
            SeatId = Guid.NewGuid(),
            Price = 50.00m,
            Order = order
        };

        // Act
        order.Items.Add(orderItem);

        // Assert
        order.Items.Should().Contain(orderItem);
        orderItem.OrderId.Should().Be(order.Id);
        orderItem.Order.Should().Be(order);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(0.01)]
    [InlineData(50.00)]
    [InlineData(99.99)]
    [InlineData(1000.00)]
    public void OrderItem_ShouldAcceptValidPrices(decimal price)
    {
        // Arrange
        var orderItem = new OrderItem();

        // Act
        orderItem.Price = price;

        // Assert
        orderItem.Price.Should().Be(price);
    }
}