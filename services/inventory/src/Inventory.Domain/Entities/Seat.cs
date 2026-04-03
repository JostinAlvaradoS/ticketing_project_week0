namespace Inventory.Domain.Entities;

public class Seat
{
    public Guid Id { get; set; }
    public string Section { get; set; } = string.Empty;
    public string Row { get; set; } = string.Empty;
    public int Number { get; set; }
    public bool Reserved { get; private set; }
    public byte[]? Version { get; set; }

    /// <summary>
    /// Marca el asiento como reservado. Lanza si ya está reservado.
    /// </summary>
    public void Reserve()
    {
        if (Reserved)
            throw new InvalidOperationException($"Seat {Id} is already reserved.");
        Reserved = true;
    }

    /// <summary>
    /// Libera el asiento, volviéndolo disponible.
    /// </summary>
    public void Release()
    {
        Reserved = false;
    }
}
