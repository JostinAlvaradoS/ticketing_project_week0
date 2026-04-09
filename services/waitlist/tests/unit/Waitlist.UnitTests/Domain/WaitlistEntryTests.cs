// TDD Cycle 1-6: Domain tests for WaitlistEntry
// STATUS: 🔴 RED — WaitlistEntry does not exist yet; all tests will fail to compile

using FluentAssertions;
using Waitlist.Domain.Entities;

namespace Waitlist.UnitTests.Domain;

public class WaitlistEntryTests
{
    // ─────────────────────────────────────────────────────────────
    // Ciclo 1: Create happy path
    // Spec: US1 — registro básico
    // ─────────────────────────────────────────────────────────────

    [Fact]
    public void Create_WithValidEmailAndEventId_ReturnsPendingEntry()
    {
        // Arrange
        var email   = "jostin@example.com";
        var eventId = Guid.NewGuid();

        // Act
        var entry = WaitlistEntry.Create(email, eventId);

        // Assert
        entry.Id.Should().NotBe(Guid.Empty);
        entry.Email.Should().Be(email);
        entry.EventId.Should().Be(eventId);
        entry.Status.Should().Be(WaitlistEntry.StatusPending);
        entry.RegisteredAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(2));
        entry.SeatId.Should().BeNull();
        entry.OrderId.Should().BeNull();
        entry.AssignedAt.Should().BeNull();
        entry.ExpiresAt.Should().BeNull();
    }

    // ─────────────────────────────────────────────────────────────
    // Ciclo 2: Create guards
    // ─────────────────────────────────────────────────────────────

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithBlankEmail_ThrowsArgumentException(string email)
    {
        // Act
        var act = () => WaitlistEntry.Create(email, Guid.NewGuid());

        // Assert
        act.Should().Throw<ArgumentException>()
           .WithMessage("*email*");
    }

    [Fact]
    public void Create_WithEmptyEventId_ThrowsArgumentException()
    {
        // Act
        var act = () => WaitlistEntry.Create("valid@example.com", Guid.Empty);

        // Assert
        act.Should().Throw<ArgumentException>()
           .WithMessage("*eventId*");
    }

    // ─────────────────────────────────────────────────────────────
    // Ciclo 3: Assign happy path
    // Spec: US2 — asignación automática
    // ─────────────────────────────────────────────────────────────

    [Fact]
    public void Assign_WhenPending_SetsStatusAssignedAndTimestamps()
    {
        // Arrange
        var entry   = WaitlistEntry.Create("jostin@example.com", Guid.NewGuid());
        var seatId  = Guid.NewGuid();
        var orderId = Guid.NewGuid();

        // Act
        entry.Assign(seatId, orderId);

        // Assert
        entry.Status.Should().Be(WaitlistEntry.StatusAssigned);
        entry.SeatId.Should().Be(seatId);
        entry.OrderId.Should().Be(orderId);
        entry.AssignedAt.Should().NotBeNull();
        entry.AssignedAt!.Value.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(2));
        entry.ExpiresAt.Should().NotBeNull();
        entry.ExpiresAt!.Value.Should().BeCloseTo(
            entry.AssignedAt.Value.AddMinutes(2),   // 2min en demo — ver 08-design-vs-implementation.md divergencia #1
            TimeSpan.FromSeconds(1));
    }

    // ─────────────────────────────────────────────────────────────
    // Ciclo 4: Assign guard de estado
    // ─────────────────────────────────────────────────────────────

    [Theory]
    [InlineData(WaitlistEntry.StatusAssigned)]
    [InlineData(WaitlistEntry.StatusExpired)]
    [InlineData(WaitlistEntry.StatusCompleted)]
    public void Assign_WhenNotPending_ThrowsInvalidOperationException(string invalidStatus)
    {
        // Arrange
        var entry = BuildEntryWithStatus(invalidStatus);

        // Act
        var act = () => entry.Assign(Guid.NewGuid(), Guid.NewGuid());

        // Assert
        act.Should().Throw<InvalidOperationException>()
           .WithMessage($"*{invalidStatus}*");
    }

    // ─────────────────────────────────────────────────────────────
    // Ciclo 5a: Complete happy path
    // Spec: US2 Scenario 3 — pago exitoso
    // ─────────────────────────────────────────────────────────────

    [Fact]
    public void Complete_WhenAssigned_SetsStatusCompleted()
    {
        // Arrange
        var entry = WaitlistEntry.Create("jostin@example.com", Guid.NewGuid());
        entry.Assign(Guid.NewGuid(), Guid.NewGuid());

        // Act
        entry.Complete();

        // Assert
        entry.Status.Should().Be(WaitlistEntry.StatusCompleted);
    }

    [Theory]
    [InlineData(WaitlistEntry.StatusPending)]
    [InlineData(WaitlistEntry.StatusExpired)]
    [InlineData(WaitlistEntry.StatusCompleted)]
    public void Complete_WhenNotAssigned_ThrowsInvalidOperationException(string invalidStatus)
    {
        // Arrange
        var entry = BuildEntryWithStatus(invalidStatus);

        // Act
        var act = () => entry.Complete();

        // Assert
        act.Should().Throw<InvalidOperationException>()
           .WithMessage($"*{invalidStatus}*");
    }

    // ─────────────────────────────────────────────────────────────
    // Ciclo 5b: Expire happy path
    // Spec: US3 — rotación por inacción
    // ─────────────────────────────────────────────────────────────

    [Fact]
    public void Expire_WhenAssigned_SetsStatusExpired()
    {
        // Arrange
        var entry = WaitlistEntry.Create("jostin@example.com", Guid.NewGuid());
        entry.Assign(Guid.NewGuid(), Guid.NewGuid());

        // Act
        entry.Expire();

        // Assert
        entry.Status.Should().Be(WaitlistEntry.StatusExpired);
    }

    [Theory]
    [InlineData(WaitlistEntry.StatusPending)]
    [InlineData(WaitlistEntry.StatusExpired)]
    [InlineData(WaitlistEntry.StatusCompleted)]
    public void Expire_WhenNotAssigned_ThrowsInvalidOperationException(string invalidStatus)
    {
        // Arrange
        var entry = BuildEntryWithStatus(invalidStatus);

        // Act
        var act = () => entry.Expire();

        // Assert
        act.Should().Throw<InvalidOperationException>()
           .WithMessage($"*{invalidStatus}*");
    }

    // ─────────────────────────────────────────────────────────────
    // Ciclo 6: IsAssignmentExpired
    // ─────────────────────────────────────────────────────────────

    [Fact]
    public void IsAssignmentExpired_WhenExpiresAtInPast_ReturnsTrue()
    {
        // Arrange
        var entry = CreateAssignedEntryWithExpiresAt(DateTime.UtcNow.AddMinutes(-1));

        // Act & Assert
        entry.IsAssignmentExpired().Should().BeTrue();
    }

    [Fact]
    public void IsAssignmentExpired_WhenExpiresAtInFuture_ReturnsFalse()
    {
        // Arrange
        var entry = WaitlistEntry.Create("jostin@example.com", Guid.NewGuid());
        entry.Assign(Guid.NewGuid(), Guid.NewGuid()); // ExpiresAt = now + 30 min

        // Act & Assert
        entry.IsAssignmentExpired().Should().BeFalse();
    }

    [Fact]
    public void IsAssignmentExpired_WhenStatusIsPending_ReturnsFalse()
    {
        // Arrange
        var entry = WaitlistEntry.Create("jostin@example.com", Guid.NewGuid());

        // Act & Assert
        entry.IsAssignmentExpired().Should().BeFalse();
    }

    // ─────────────────────────────────────────────────────────────
    // Helpers — construyen estados sin pasar por transiciones normales
    // (usando reflection para tests de guard)
    // ─────────────────────────────────────────────────────────────

    private static WaitlistEntry BuildEntryWithStatus(string status)
    {
        var entry = WaitlistEntry.Create("jostin@example.com", Guid.NewGuid());

        // Force status via reflection to test guards without valid transitions
        var statusProp = typeof(WaitlistEntry)
            .GetProperty(nameof(WaitlistEntry.Status))!;
        statusProp.SetValue(entry, status);

        return entry;
    }

    private static WaitlistEntry CreateAssignedEntryWithExpiresAt(DateTime expiresAt)
    {
        var entry = WaitlistEntry.Create("jostin@example.com", Guid.NewGuid());
        entry.Assign(Guid.NewGuid(), Guid.NewGuid());

        // Override ExpiresAt to simulate expired assignment
        var expiresAtProp = typeof(WaitlistEntry)
            .GetProperty(nameof(WaitlistEntry.ExpiresAt))!;
        expiresAtProp.SetValue(entry, expiresAt);

        return entry;
    }
}
