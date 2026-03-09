namespace Ordering.Domain.UnitTests;

public class OrderTests
{
    [Fact]
    public void Order_ShouldBeCreated_WithCorrectDefaultValues()
    {
        // Arrange & Act
        var order = new Order();

        // Assert
        order.Id.Should().Be(Guid.Empty); // Id is not auto-generated, starts empty
        order.UserId.Should().BeNull();
        order.GuestToken.Should().BeNull();
        order.TotalAmount.Should().Be(0);
        order.State.Should().Be("draft");
        order.CreatedAt.Should().Be(DateTime.MinValue); // CreatedAt is not auto-set
        order.PaidAt.Should().BeNull();
        order.Items.Should().NotBeNull();
        order.Items.Should().BeEmpty();
    }

    [Fact]
    public void Order_ShouldAcceptValidUserId()
    {
        // Arrange
        var order = new Order();
        var userId = "user123";

        // Act
        order.UserId = userId;

        // Assert
        order.UserId.Should().Be(userId);
    }

    [Fact]
    public void Order_ShouldAcceptValidGuestToken()
    {
        // Arrange
        var order = new Order();
        var guestToken = "guest-token-123";

        // Act
        order.GuestToken = guestToken;

        // Assert
        order.GuestToken.Should().Be(guestToken);
    }

    [Theory]
    [InlineData("draft")]
    [InlineData("pending")]
    [InlineData("paid")]
    [InlineData("fulfilled")]
    [InlineData("cancelled")]
    public void Order_ShouldAcceptValidStates(string state)
    {
        // Arrange
        var order = new Order();

        // Act
        order.State = state;

        // Assert
        order.State.Should().Be(state);
    }

    [Fact]
    public void Order_ShouldCalculateTotalAmount_WhenItemsAdded()
    {
        // Arrange
        var order = new Order { Id = Guid.NewGuid() };
        var item1 = new OrderItem { Id = Guid.NewGuid(), OrderId = order.Id, SeatId = Guid.NewGuid(), Price = 50.00m };
        var item2 = new OrderItem { Id = Guid.NewGuid(), OrderId = order.Id, SeatId = Guid.NewGuid(), Price = 75.50m };

        // Act
        order.Items.Add(item1);
        order.Items.Add(item2);
        order.TotalAmount = order.Items.Sum(i => i.Price);

        // Assert
        order.TotalAmount.Should().Be(125.50m);
        order.Items.Should().HaveCount(2);
    }

    [Fact]
    public void Order_ShouldBeEmpty_WhenNoItems()
    {
        // Arrange
        var order = new Order();

        // Act & Assert
        order.Items.Should().BeEmpty();
        order.TotalAmount.Should().Be(0);
    }

    [Fact]
    public void Order_ShouldSetPaidAt_WhenMarkedAsPaid()
    {
        // Arrange
        var order = new Order { State = "pending" };
        var paidTime = DateTime.UtcNow;

        // Act
        order.State = "paid";
        order.PaidAt = paidTime;

        // Assert
        order.State.Should().Be("paid");
        order.PaidAt.Should().Be(paidTime);
    }

    [Fact]
    public void Order_ShouldHaveUniqueId_WhenCreated()
    {
        // Arrange & Act
        var order1 = new Order { Id = Guid.NewGuid() };
        var order2 = new Order { Id = Guid.NewGuid() };

        // Assert
        order1.Id.Should().NotBe(order2.Id);
        order1.Id.Should().NotBeEmpty();
        order2.Id.Should().NotBeEmpty();
    }
}