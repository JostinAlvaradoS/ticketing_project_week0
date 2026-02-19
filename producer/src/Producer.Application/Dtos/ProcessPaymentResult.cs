namespace Producer.Application.Dtos;

public class ProcessPaymentResult
{
    public bool Success { get; set; }
    public int TicketId { get; set; }
    public int EventId { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? Message { get; set; }
}
