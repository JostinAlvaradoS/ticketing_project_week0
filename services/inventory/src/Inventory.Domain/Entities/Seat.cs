namespace Inventory.Domain.Entities;

public class Seat
{
    public Guid Id { get; set; }
    public string Section { get; set; } = string.Empty;
    public string Row { get; set; } = string.Empty;
    public int Number { get; set; }
    public bool Reserved { get; set; }
    public byte[]? Version { get; set; }
}
