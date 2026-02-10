using PaymentService.Api.Models.Entities;

namespace PaymentService.Api.Models.DTOs;

public class PaymentResponse
{
    public long Id { get; set; }
    public long TicketId { get; set; }
    public PaymentStatus Status { get; set; }
    public string? ProviderRef { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}