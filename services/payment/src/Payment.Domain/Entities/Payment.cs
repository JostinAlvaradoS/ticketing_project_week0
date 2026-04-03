namespace Payment.Domain.Entities;

public class Payment
{
    public const string StatusPending = "pending";
    public const string StatusSucceeded = "succeeded";
    public const string StatusFailed = "failed";
    public const string StatusCancelled = "cancelled";

    public Guid Id { get; set; }
    public Guid OrderId { get; set; }
    public Guid CustomerId { get; set; }
    public Guid? ReservationId { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "USD";
    public string PaymentMethod { get; set; } = null!;
    public string Status { get; set; } = StatusPending;
    public string? ErrorCode { get; set; }
    public string? ErrorMessage { get; set; }
    public string? FailureReason { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ProcessedAt { get; set; }
    
    // Metadata for simulated payments
    public bool IsSimulated { get; set; } = true;
    public string? SimulatedResponse { get; set; }

    public void MarkAsSucceeded()
    {
        if (Status != StatusPending)
            throw new InvalidOperationException($"Cannot succeed payment from status: {Status}");

        Status = StatusSucceeded;
        ProcessedAt = DateTime.UtcNow;
    }

    public void MarkAsFailed(string errorCode, string errorMessage)
    {
        if (Status != StatusPending)
            throw new InvalidOperationException($"Cannot fail payment from status: {Status}");

        Status = StatusFailed;
        ErrorCode = errorCode;
        ErrorMessage = errorMessage;
        ProcessedAt = DateTime.UtcNow;
    }

    public void Cancel()
    {
        if (Status == StatusSucceeded || Status == StatusCancelled)
            throw new InvalidOperationException($"Cannot cancel a payment in status '{Status}'.");

        Status = StatusCancelled;
        ProcessedAt = DateTime.UtcNow;
    }

    public bool IsValidForProcess()
    {
        return Amount > 0 && 
               OrderId != Guid.Empty && 
               CustomerId != Guid.Empty && 
               !string.IsNullOrWhiteSpace(PaymentMethod);
    }
}