namespace Ordering.Domain.UnitTests;

public class OrderItemTests
{
    [Fact]
    public void OrderItem_ShouldBeCreated_WithCorrectProperties()
    {
        var order = Order.Create("user-1", null);
        var seatId = Guid.NewGuid();

        var item = order.AddItem(seatId, 99.99m);

        item.Id.Should().NotBeEmpty();
        item.OrderId.Should().Be(order.Id);
        item.SeatId.Should().Be(seatId);
        item.Price.Should().Be(99.99m);
    }

    [Fact]
    public void OrderItem_ShouldHaveUniqueId_WhenCreated()
    {
        var order = Order.Create("user-1", null);

        var item1 = order.AddItem(Guid.NewGuid(), 50.00m);
        var item2 = order.AddItem(Guid.NewGuid(), 75.00m);

        item1.Id.Should().NotBe(item2.Id);
        item1.Id.Should().NotBeEmpty();
        item2.Id.Should().NotBeEmpty();
    }

    [Fact]
    public void OrderItem_ShouldAcceptPositivePrice()
    {
        var order = Order.Create("user-1", null);
        var price = 150.75m;

        var item = order.AddItem(Guid.NewGuid(), price);

        item.Price.Should().Be(price);
    }

    [Fact]
    public void OrderItem_ShouldSetNavigationProperty_WhenOrderAssigned()
    {
        var order = Order.Create("user-1", null);
        var seatId = Guid.NewGuid();

        var item = order.AddItem(seatId, 50.00m);

        item.OrderId.Should().Be(order.Id);
        order.Items.Should().Contain(item);
    }

    [Fact]
    public void OrderItem_ShouldBelongToOrder_WhenAddedViaAddItem()
    {
        var order = Order.Create("user-1", null);
        var seatId = Guid.NewGuid();

        var item = order.AddItem(seatId, 50.00m);

        order.Items.Should().HaveCount(1);
        order.Items.First().SeatId.Should().Be(seatId);
        item.OrderId.Should().Be(order.Id);
    }

    [Theory]
    [InlineData(0.01)]
    [InlineData(50.00)]
    [InlineData(99.99)]
    [InlineData(1000.00)]
    public void OrderItem_ShouldAcceptValidPrices(decimal price)
    {
        var order = Order.Create("user-1", null);

        var item = order.AddItem(Guid.NewGuid(), price);

        item.Price.Should().Be(price);
    }
}
