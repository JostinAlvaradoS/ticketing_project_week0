namespace Waitlist.Domain.Entities;

public class WaitlistEntry
{
    public const string StatusPending = "Pending";
    public const string StatusAssigned = "Assigned";
    public const string StatusExpired = "Expired";
    public const string StatusCompleted = "Completed";

    private static readonly TimeSpan AssignmentTtl = TimeSpan.FromHours(24);

    public Guid Id { get; private set; }
    public string Email { get; private set; } = string.Empty;
    public Guid EventId { get; private set; }
    public string Status { get; private set; } = StatusPending;
    public DateTime CreatedAt { get; private set; }
    public DateTime? AssignedAt { get; private set; }
    public DateTime? ExpiresAt { get; private set; }
    public DateTime? CompletedAt { get; private set; }
    public Guid? SeatId { get; private set; }
    public Guid? OrderId { get; private set; }

    // EF Core constructor
    private WaitlistEntry() { }

    public static WaitlistEntry Create(string email, Guid eventId)
    {
        if (string.IsNullOrWhiteSpace(email))
            throw new ArgumentException("Email cannot be blank.", nameof(email));
        if (eventId == Guid.Empty)
            throw new ArgumentException("EventId cannot be empty.", nameof(eventId));

        return new WaitlistEntry
        {
            Id = Guid.NewGuid(),
            Email = email,
            EventId = eventId,
            Status = StatusPending,
            CreatedAt = DateTime.UtcNow
        };
    }

    public void Assign(Guid seatId, Guid orderId)
    {
        if (Status != StatusPending)
            throw new InvalidOperationException(
                $"Cannot assign entry in status '{Status}'. Only Pending entries can be assigned.");

        SeatId = seatId;
        OrderId = orderId;
        Status = StatusAssigned;
        AssignedAt = DateTime.UtcNow;
        ExpiresAt = DateTime.UtcNow.Add(AssignmentTtl);
    }

    public void Complete()
    {
        if (Status != StatusAssigned)
            throw new InvalidOperationException(
                $"Cannot complete entry in status '{Status}'. Only Assigned entries can be completed.");

        Status = StatusCompleted;
        CompletedAt = DateTime.UtcNow;
    }

    public void Expire()
    {
        if (Status != StatusAssigned)
            throw new InvalidOperationException(
                $"Cannot expire entry in status '{Status}'. Only Assigned entries can be expired.");

        Status = StatusExpired;
    }

    public bool IsAssignmentExpired()
    {
        if (Status != StatusAssigned || ExpiresAt is null)
            return false;

        return ExpiresAt <= DateTime.UtcNow;
    }
}
