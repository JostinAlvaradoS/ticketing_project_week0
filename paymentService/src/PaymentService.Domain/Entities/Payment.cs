using PaymentService.Domain.Enums;

namespace PaymentService.Domain.Entities;

public class Payment
{
    public long Id { get; set; }
    public long TicketId { get; set; }
    public PaymentStatus Status { get; set; }
    public string? ProviderRef { get; set; }
    public int AmountCents { get; set; }
    public string Currency { get; set; } = "USD";
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public Ticket Ticket { get; set; } = null!;
}
