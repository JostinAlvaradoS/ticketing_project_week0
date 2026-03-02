using System.Text.Json.Serialization;

namespace Fulfillment.Infrastructure.Events;

public class PaymentSucceededEvent
{
    [JsonPropertyName("orderId")]
    public Guid OrderId { get; set; }

    [JsonPropertyName("customer_email")]
    public string CustomerEmail { get; set; } = string.Empty;

    [JsonPropertyName("event_id")]
    public Guid EventId { get; set; }

    [JsonPropertyName("event_name")]
    public string EventName { get; set; } = string.Empty;

    [JsonPropertyName("seat_number")]
    public string SeatNumber { get; set; } = string.Empty;

    [JsonPropertyName("price")]
    public decimal Price { get; set; }

    [JsonPropertyName("currency")]
    public string Currency { get; set; } = "USD";

    [JsonPropertyName("paymentId")]
    public Guid PaymentId { get; set; }

    [JsonPropertyName("timestamp")]
    public DateTime Timestamp { get; set; }
}

public class TicketIssuedEvent
{
    [JsonPropertyName("order_id")]
    public Guid OrderId { get; set; }

    [JsonPropertyName("ticket_id")]
    public Guid TicketId { get; set; }

    [JsonPropertyName("customer_email")]
    public string CustomerEmail { get; set; } = string.Empty;

    [JsonPropertyName("ticket_pdf_url")]
    public string TicketPdfUrl { get; set; } = string.Empty;

    [JsonPropertyName("event_name")]
    public string EventName { get; set; } = string.Empty;

    [JsonPropertyName("seat_number")]
    public string SeatNumber { get; set; } = string.Empty;

    [JsonPropertyName("timestamp")]
    public DateTime Timestamp { get; set; }
}
