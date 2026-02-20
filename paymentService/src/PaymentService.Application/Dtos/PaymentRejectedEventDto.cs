namespace PaymentService.Application.Dtos;

public class PaymentRejectedEventDto
{
    public long TicketId { get; set; }
    public long PaymentId { get; set; }
    public string? ProviderReference { get; set; }
    public string RejectionReason { get; set; } = string.Empty;
    public DateTime RejectedAt { get; set; }
    public long EventId { get; set; }
    public DateTime EventTimestamp { get; set; }
}
