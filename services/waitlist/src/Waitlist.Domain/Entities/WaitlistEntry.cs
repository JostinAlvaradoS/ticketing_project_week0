// TDD Ciclos 1-6 — GREEN: mínimo necesario para pasar todos los tests de dominio

namespace Waitlist.Domain.Entities;

public class WaitlistEntry
{
    public const string StatusPending   = "pending";
    public const string StatusAssigned  = "assigned";
    public const string StatusExpired   = "expired";
    public const string StatusCompleted = "completed";

    public Guid      Id           { get; private set; }
    public string    Email        { get; private set; } = string.Empty;
    public Guid      EventId      { get; private set; }
    public Guid?     SeatId       { get; private set; }
    public Guid?     OrderId      { get; private set; }
    public string    Status       { get; private set; } = StatusPending;
    public DateTime  RegisteredAt { get; private set; }
    public DateTime? AssignedAt   { get; private set; }
    public DateTime? ExpiresAt    { get; private set; }

    private WaitlistEntry() { }

    // ── Ciclo 1 + 2: factory con guards de validación ──────────────

    public static WaitlistEntry Create(string email, Guid eventId)
    {
        if (string.IsNullOrWhiteSpace(email))
            throw new ArgumentException("email must not be blank.", nameof(email));

        if (eventId == Guid.Empty)
            throw new ArgumentException("eventId must not be empty.", nameof(eventId));

        return new WaitlistEntry
        {
            Id           = Guid.NewGuid(),
            Email        = email,
            EventId      = eventId,
            Status       = StatusPending,
            RegisteredAt = DateTime.UtcNow
        };
    }

    // ── Ciclo 3 + 4: Assign con guard de estado ────────────────────

    public void Assign(Guid seatId, Guid orderId)
    {
        if (Status != StatusPending)
            throw new InvalidOperationException(
                $"Cannot assign entry in status '{Status}'.");

        SeatId     = seatId;
        OrderId    = orderId;
        Status     = StatusAssigned;
        AssignedAt = DateTime.UtcNow;
        ExpiresAt  = AssignedAt.Value.AddMinutes(30);
    }

    // ── Ciclo 5a: Complete ─────────────────────────────────────────

    public void Complete()
    {
        if (Status != StatusAssigned)
            throw new InvalidOperationException(
                $"Cannot complete entry in status '{Status}'.");

        Status = StatusCompleted;
    }

    // ── Ciclo 5b: Expire ───────────────────────────────────────────

    public void Expire()
    {
        if (Status != StatusAssigned)
            throw new InvalidOperationException(
                $"Cannot expire entry in status '{Status}'.");

        Status = StatusExpired;
    }

    // ── Ciclo 6: IsAssignmentExpired ───────────────────────────────

    public bool IsAssignmentExpired() =>
        Status == StatusAssigned && ExpiresAt.HasValue && DateTime.UtcNow > ExpiresAt.Value;
}
