using FluentAssertions;
using Waitlist.Domain.Entities;

namespace Waitlist.UnitTests.Domain;

public class WaitlistEntryTests
{
    // Cycle 1 — Create happy path
    [Fact]
    public void Create_WithValidEmailAndEventId_ReturnsPendingEntry()
    {
        var eventId = Guid.NewGuid();
        var entry = WaitlistEntry.Create("user@example.com", eventId);

        entry.Id.Should().NotBeEmpty();
        entry.Email.Should().Be("user@example.com");
        entry.EventId.Should().Be(eventId);
        entry.Status.Should().Be(WaitlistEntry.StatusPending);
        entry.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    // Cycle 2 — Create guards
    [Fact]
    public void Create_WithBlankEmail_ThrowsArgumentException()
    {
        var act = () => WaitlistEntry.Create("   ", Guid.NewGuid());
        act.Should().Throw<ArgumentException>().WithParameterName("email");
    }

    [Fact]
    public void Create_WithEmptyEventId_ThrowsArgumentException()
    {
        var act = () => WaitlistEntry.Create("user@example.com", Guid.Empty);
        act.Should().Throw<ArgumentException>().WithParameterName("eventId");
    }

    // Cycle 3 — Assign happy path
    [Fact]
    public void Assign_WhenPending_SetsStatusAssignedAndTimestamps()
    {
        var entry = WaitlistEntry.Create("user@example.com", Guid.NewGuid());
        var seatId = Guid.NewGuid();
        var orderId = Guid.NewGuid();

        entry.Assign(seatId, orderId);

        entry.Status.Should().Be(WaitlistEntry.StatusAssigned);
        entry.SeatId.Should().Be(seatId);
        entry.OrderId.Should().Be(orderId);
        entry.AssignedAt.Should().NotBeNull();
        entry.ExpiresAt.Should().NotBeNull();
        entry.ExpiresAt.Should().BeAfter(DateTime.UtcNow);
    }

    // Cycle 4 — Assign guard
    [Theory]
    [InlineData(WaitlistEntry.StatusAssigned)]
    [InlineData(WaitlistEntry.StatusExpired)]
    [InlineData(WaitlistEntry.StatusCompleted)]
    public void Assign_WhenNotPending_ThrowsInvalidOperationException(string status)
    {
        var entry = WaitlistEntry.Create("user@example.com", Guid.NewGuid());
        // Force status via reflection for non-Pending states
        typeof(WaitlistEntry)
            .GetProperty(nameof(WaitlistEntry.Status))!
            .SetValue(entry, status);

        var act = () => entry.Assign(Guid.NewGuid(), Guid.NewGuid());
        act.Should().Throw<InvalidOperationException>();
    }

    // Cycle 5 — Complete and Expire
    [Fact]
    public void Complete_WhenAssigned_SetsStatusCompleted()
    {
        var entry = WaitlistEntry.Create("user@example.com", Guid.NewGuid());
        entry.Assign(Guid.NewGuid(), Guid.NewGuid());

        entry.Complete();

        entry.Status.Should().Be(WaitlistEntry.StatusCompleted);
        entry.CompletedAt.Should().NotBeNull();
    }

    [Fact]
    public void Expire_WhenAssigned_SetsStatusExpired()
    {
        var entry = WaitlistEntry.Create("user@example.com", Guid.NewGuid());
        entry.Assign(Guid.NewGuid(), Guid.NewGuid());

        entry.Expire();

        entry.Status.Should().Be(WaitlistEntry.StatusExpired);
    }

    [Theory]
    [InlineData(WaitlistEntry.StatusPending)]
    [InlineData(WaitlistEntry.StatusExpired)]
    [InlineData(WaitlistEntry.StatusCompleted)]
    public void Complete_WhenNotAssigned_ThrowsInvalidOperationException(string status)
    {
        var entry = WaitlistEntry.Create("user@example.com", Guid.NewGuid());
        typeof(WaitlistEntry)
            .GetProperty(nameof(WaitlistEntry.Status))!
            .SetValue(entry, status);

        var act = () => entry.Complete();
        act.Should().Throw<InvalidOperationException>();
    }

    [Theory]
    [InlineData(WaitlistEntry.StatusPending)]
    [InlineData(WaitlistEntry.StatusCompleted)]
    public void Expire_WhenNotAssigned_ThrowsInvalidOperationException(string status)
    {
        var entry = WaitlistEntry.Create("user@example.com", Guid.NewGuid());
        typeof(WaitlistEntry)
            .GetProperty(nameof(WaitlistEntry.Status))!
            .SetValue(entry, status);

        var act = () => entry.Expire();
        act.Should().Throw<InvalidOperationException>();
    }

    // Cycle 6 — IsAssignmentExpired
    [Fact]
    public void IsAssignmentExpired_WhenExpiresAtInPast_ReturnsTrue()
    {
        var entry = WaitlistEntry.Create("user@example.com", Guid.NewGuid());
        entry.Assign(Guid.NewGuid(), Guid.NewGuid());

        // Force ExpiresAt to past
        typeof(WaitlistEntry)
            .GetProperty(nameof(WaitlistEntry.ExpiresAt))!
            .SetValue(entry, DateTime.UtcNow.AddHours(-1));

        entry.IsAssignmentExpired().Should().BeTrue();
    }

    [Fact]
    public void IsAssignmentExpired_WhenExpiresAtInFuture_ReturnsFalse()
    {
        var entry = WaitlistEntry.Create("user@example.com", Guid.NewGuid());
        entry.Assign(Guid.NewGuid(), Guid.NewGuid());

        entry.IsAssignmentExpired().Should().BeFalse();
    }

    [Fact]
    public void IsAssignmentExpired_WhenNotAssigned_ReturnsFalse()
    {
        var entry = WaitlistEntry.Create("user@example.com", Guid.NewGuid());

        entry.IsAssignmentExpired().Should().BeFalse();
    }
}
