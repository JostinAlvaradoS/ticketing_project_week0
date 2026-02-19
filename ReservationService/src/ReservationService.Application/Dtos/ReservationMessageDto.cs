namespace ReservationService.Application.Dtos;

public class ReservationMessageDto
{
    public long TicketId { get; set; }
    public long EventId { get; set; }
    public string OrderId { get; set; } = string.Empty;
    public string ReservedBy { get; set; } = string.Empty;
    public int ReservationDurationSeconds { get; set; } = 300;
    public DateTime PublishedAt { get; set; }
}
