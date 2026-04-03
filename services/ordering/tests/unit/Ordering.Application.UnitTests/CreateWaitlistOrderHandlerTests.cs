using FluentAssertions;
using Moq;
using Ordering.Application.Exceptions;
using Ordering.Application.Ports;
using Ordering.Application.UseCases.CreateWaitlistOrder;
using Ordering.Domain.Entities;

namespace Ordering.Application.UnitTests.UseCases.CreateWaitlistOrder;

/// <summary>
/// TDD Ciclo 22 — RED: POST /orders/waitlist handler tests.
/// </summary>
public class CreateWaitlistOrderHandlerTests
{
    private readonly Mock<IOrderRepository> _repositoryMock;
    private readonly CreateWaitlistOrderHandler _handler;

    public CreateWaitlistOrderHandlerTests()
    {
        _repositoryMock = new Mock<IOrderRepository>();
        _handler = new CreateWaitlistOrderHandler(_repositoryMock.Object);
    }

    [Fact]
    public async Task Handle_ValidRequest_CreatesOrderAndReturnsOrderId()
    {
        // Arrange
        var seatId = Guid.NewGuid();
        var command = new CreateWaitlistOrderCommand(seatId, "user@example.com", 99.99m);

        _repositoryMock
            .Setup(r => r.GetActiveOrderBySeatIdAsync(seatId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Order?)null); // no duplicate

        _repositoryMock
            .Setup(r => r.CreateAsync(It.IsAny<Order>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Order order, CancellationToken _) => order);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.OrderId.Should().NotBeEmpty();
        _repositoryMock.Verify(r => r.CreateAsync(
            It.Is<Order>(o => o.State == Order.StatePending && o.GuestToken == "user@example.com"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_DuplicateSeatId_ThrowsDuplicateSeatOrderException()
    {
        // Arrange
        var seatId = Guid.NewGuid();
        var command = new CreateWaitlistOrderCommand(seatId, "user@example.com", 99.99m);

        var existingOrder = Order.Create(null, "other@example.com");
        existingOrder.AddItem(seatId, 50m);
        existingOrder.Checkout();

        _repositoryMock
            .Setup(r => r.GetActiveOrderBySeatIdAsync(seatId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingOrder); // duplicate!

        // Act & Assert — handler throws; controller maps this to 409
        await Assert.ThrowsAsync<DuplicateSeatOrderException>(() =>
            _handler.Handle(command, CancellationToken.None));

        _repositoryMock.Verify(r => r.CreateAsync(It.IsAny<Order>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
