namespace Producer.Application.Dtos;

public class ReserveTicketResult
{
    public bool Success { get; set; }
    public long TicketId { get; set; }
    public string? Message { get; set; }
}
