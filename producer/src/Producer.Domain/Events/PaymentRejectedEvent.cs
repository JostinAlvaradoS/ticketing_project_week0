namespace Producer.Domain.Events;

public class PaymentRejectedEvent
{
    public int TicketId { get; set; }
    public int EventId { get; set; }
    public int AmountCents { get; set; }
    public string Currency { get; set; } = "USD";
    public required string PaymentBy { get; set; }
    public required string RejectionReason { get; set; }
    public string? TransactionRef { get; set; }
    public DateTime RejectedAt { get; set; }
}
