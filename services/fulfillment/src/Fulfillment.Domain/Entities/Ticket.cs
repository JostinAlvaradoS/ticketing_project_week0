namespace Fulfillment.Domain.Entities;

public class Ticket
{
    public Guid Id { get; set; }
    public Guid OrderId { get; set; }
    public string CustomerEmail { get; set; } = string.Empty;
    public string EventName { get; set; } = string.Empty;
    public string SeatNumber { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string Currency { get; set; } = "USD";
    public TicketStatus Status { get; set; }
    public string QrCodeData { get; set; } = string.Empty;
    public string TicketPdfPath { get; set; } = string.Empty;
    public DateTime GeneratedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public enum TicketStatus
{
    Pending = 0,
    Generated = 1,
    Failed = 2,
    Delivered = 3
}
