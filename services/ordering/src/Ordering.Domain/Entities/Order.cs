using System.ComponentModel.DataAnnotations;

namespace Ordering.Domain.Entities;

public class Order
{
    public Guid Id { get; set; }
    public string? UserId { get; set; }
    public string? GuestToken { get; set; }
    public decimal TotalAmount { get; set; }
    public string State { get; set; } = "draft"; // draft, pending, paid, fulfilled, cancelled
    public DateTime CreatedAt { get; set; }
    public DateTime? PaidAt { get; set; }
    public ICollection<OrderItem> Items { get; set; } = new List<OrderItem>();
}

public class OrderItem
{
    public Guid Id { get; set; }
    public Guid OrderId { get; set; }
    public Guid SeatId { get; set; }
    public decimal Price { get; set; }
    public Order Order { get; set; } = null!;
}