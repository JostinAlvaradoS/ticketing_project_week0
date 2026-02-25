namespace Payment.Domain.Entities;

public class Payment
{
    public Guid Id { get; set; }
    public Guid OrderId { get; set; }
    public Guid CustomerId { get; set; }
    public Guid? ReservationId { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "USD";
    public string PaymentMethod { get; set; } = null!;
    public string Status { get; set; } = "pending"; // pending, succeeded, failed
    public string? ErrorCode { get; set; }
    public string? ErrorMessage { get; set; }
    public string? FailureReason { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ProcessedAt { get; set; }
    
    // Metadata for simulated payments
    public bool IsSimulated { get; set; } = true;
    public string? SimulatedResponse { get; set; }
}