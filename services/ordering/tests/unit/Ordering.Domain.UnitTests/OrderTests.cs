namespace Ordering.Domain.UnitTests;

public class OrderTests
{
    // ─── Order.Create ────────────────────────────────────────────────────────

    [Fact]
    public void Order_Create_WithUserId_ShouldSetDraftStateAndGenerateId()
    {
        var order = Order.Create("user-123", null);

        order.Id.Should().NotBeEmpty();
        order.UserId.Should().Be("user-123");
        order.GuestToken.Should().BeNull();
        order.State.Should().Be(Order.StateDraft);
        order.TotalAmount.Should().Be(0);
        order.Items.Should().BeEmpty();
        order.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(2));
    }

    [Fact]
    public void Order_Create_WithGuestToken_ShouldSucceed()
    {
        var order = Order.Create(null, "guest-token-abc");

        order.GuestToken.Should().Be("guest-token-abc");
        order.UserId.Should().BeNull();
        order.State.Should().Be(Order.StateDraft);
    }

    [Fact]
    public void Order_Create_WithoutUserIdOrGuestToken_ShouldThrow()
    {
        var act = () => Order.Create(null, null);

        act.Should().Throw<ArgumentException>()
            .WithMessage("*UserId or GuestToken*");
    }

    // ─── Order.AddItem ────────────────────────────────────────────────────────

    [Fact]
    public void Order_AddItem_ShouldAddItemAndRecalculateTotal()
    {
        var order = Order.Create("user-1", null);
        var seatId = Guid.NewGuid();

        order.AddItem(seatId, 50.00m);

        order.Items.Should().HaveCount(1);
        order.TotalAmount.Should().Be(50.00m);
    }

    [Fact]
    public void Order_AddItem_MultipleTimes_ShouldAccumulateTotal()
    {
        var order = Order.Create("user-1", null);

        order.AddItem(Guid.NewGuid(), 50.00m);
        order.AddItem(Guid.NewGuid(), 75.50m);

        order.Items.Should().HaveCount(2);
        order.TotalAmount.Should().Be(125.50m);
    }

    [Fact]
    public void Order_AddItem_DuplicateSeat_ShouldThrow()
    {
        var order = Order.Create("user-1", null);
        var seatId = Guid.NewGuid();
        order.AddItem(seatId, 50.00m);

        var act = () => order.AddItem(seatId, 50.00m);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*already in the cart*");
    }

    [Fact]
    public void Order_AddItem_WhenNotInDraft_ShouldThrow()
    {
        var order = Order.Create("user-1", null);
        order.AddItem(Guid.NewGuid(), 50.00m);
        order.Checkout();

        var act = () => order.AddItem(Guid.NewGuid(), 30.00m);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Cannot add items*");
    }

    // ─── Order.Checkout ───────────────────────────────────────────────────────

    [Fact]
    public void Order_Checkout_WithItems_ShouldTransitionToPending()
    {
        var order = Order.Create("user-1", null);
        order.AddItem(Guid.NewGuid(), 100.00m);

        order.Checkout();

        order.State.Should().Be(Order.StatePending);
    }

    [Fact]
    public void Order_Checkout_WithoutItems_ShouldThrow()
    {
        var order = Order.Create("user-1", null);

        var act = () => order.Checkout();

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*empty*");
    }

    [Fact]
    public void Order_Checkout_AlreadyPending_ShouldThrow()
    {
        var order = Order.Create("user-1", null);
        order.AddItem(Guid.NewGuid(), 50.00m);
        order.Checkout();

        var act = () => order.Checkout();

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Cannot checkout*");
    }

    // ─── Order.MarkAsPaid ─────────────────────────────────────────────────────

    [Fact]
    public void Order_MarkAsPaid_WhenPending_ShouldTransitionToPaid()
    {
        var order = Order.Create("user-1", null);
        order.AddItem(Guid.NewGuid(), 50.00m);
        order.Checkout();

        order.MarkAsPaid();

        order.State.Should().Be(Order.StatePaid);
        order.PaidAt.Should().NotBeNull();
        order.PaidAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(2));
    }

    [Fact]
    public void Order_MarkAsPaid_WhenDraft_ShouldThrow()
    {
        var order = Order.Create("user-1", null);

        var act = () => order.MarkAsPaid();

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Cannot mark as paid*");
    }

    // ─── Order.BelongsTo ──────────────────────────────────────────────────────

    [Fact]
    public void Order_BelongsTo_CorrectUser_ShouldReturnTrue()
    {
        var order = Order.Create("user-123", null);

        order.BelongsTo("user-123", null).Should().BeTrue();
    }

    [Fact]
    public void Order_BelongsTo_WrongUser_ShouldReturnFalse()
    {
        var order = Order.Create("user-123", null);

        order.BelongsTo("user-999", null).Should().BeFalse();
    }

    [Fact]
    public void Order_BelongsTo_CorrectGuestToken_ShouldReturnTrue()
    {
        var order = Order.Create(null, "guest-abc");

        order.BelongsTo(null, "guest-abc").Should().BeTrue();
    }

    // ─── State constants ──────────────────────────────────────────────────────

    [Fact]
    public void Order_StateConstants_ShouldMatchExpectedValues()
    {
        Order.StateDraft.Should().Be("draft");
        Order.StatePending.Should().Be("pending");
        Order.StatePaid.Should().Be("paid");
        Order.StateFulfilled.Should().Be("fulfilled");
        Order.StateCancelled.Should().Be("cancelled");
    }

    // ─── Unique IDs ───────────────────────────────────────────────────────────

    [Fact]
    public void Order_Create_TwoOrders_ShouldHaveUniqueIds()
    {
        var order1 = Order.Create("user-1", null);
        var order2 = Order.Create("user-2", null);

        order1.Id.Should().NotBe(order2.Id);
        order1.Id.Should().NotBeEmpty();
    }
}
