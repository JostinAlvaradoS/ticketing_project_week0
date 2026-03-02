using FluentAssertions;
using Ordering.Application.DTOs;
using Xunit;

namespace Ordering.Application.UnitTests.DTOs;

public class OrderDtoTests
{
    [Fact]
    public void OrderDto_Should_Initialize_Correctly()
    {
        // Arrange
        var id = Guid.NewGuid();
        var userId = "user-123";
        var totalAmount = 150.0m;
        var state = "pending";
        var createdAt = DateTime.UtcNow;
        var items = new List<OrderItemDto>
        {
            new OrderItemDto(Guid.NewGuid(), Guid.NewGuid(), 75.0m),
            new OrderItemDto(Guid.NewGuid(), Guid.NewGuid(), 75.0m)
        };

        // Act
        var dto = new OrderDto(id, userId, null, totalAmount, state, createdAt, null, items);

        // Assert
        dto.Id.Should().Be(id);
        dto.UserId.Should().Be(userId);
        dto.TotalAmount.Should().Be(totalAmount);
        dto.State.Should().Be(state);
        dto.Items.Should().HaveCount(2);
    }
}
