namespace Catalog.Domain.Entities;

public class Seat
{
    public const string StatusAvailable = "available";
    public const string StatusReserved = "reserved";
    public const string StatusSold = "sold";
    public const string StatusUnavailable = "unavailable";

    public Guid Id { get; set; }
    public Guid EventId { get; set; }
    public string SectionCode { get; set; } = string.Empty;
    public int RowNumber { get; set; }
    public int SeatNumber { get; set; }
    public decimal Price { get; set; }
    public string Status { get; set; } = StatusAvailable;
    
    public Event? Event { get; set; }

    // Business Logic Methods
    public bool IsAvailable()
    {
        return Status.Equals(StatusAvailable, StringComparison.OrdinalIgnoreCase);
    }

    public bool IsReserved()
    {
        return Status.Equals(StatusReserved, StringComparison.OrdinalIgnoreCase);
    }

    public bool IsSold()
    {
        return Status.Equals(StatusSold, StringComparison.OrdinalIgnoreCase);
    }

    public bool IsUnavailable()
    {
        return Status.Equals(StatusUnavailable, StringComparison.OrdinalIgnoreCase);
    }

    public bool CanBeReserved()
    {
        return IsAvailable();
    }

    public bool CanBeSold()
    {
        return IsAvailable() || IsReserved();
    }

    public bool CanBeReleased()
    {
        return IsReserved();
    }

    public void Reserve()
    {
        if (!CanBeReserved())
            throw new InvalidOperationException($"Cannot reserve seat. Current status: {Status}");
        
        Status = StatusReserved;
    }

    public void Sell()
    {
        if (!CanBeSold())
            throw new InvalidOperationException($"Cannot sell seat. Current status: {Status}");
        
        Status = StatusSold;
    }

    public void Release()
    {
        if (!CanBeReleased())
            throw new InvalidOperationException($"Cannot release seat. Current status: {Status}");
        
        Status = StatusAvailable;
    }

    public void MakeUnavailable()
    {
        if (IsReserved() || IsSold())
            throw new InvalidOperationException($"Cannot make seat unavailable. Seat has active reservation or is sold. Current status: {Status}");
        
        Status = StatusUnavailable;
    }

    public void MakeAvailable()
    {
        if (!IsUnavailable())
            throw new InvalidOperationException($"Cannot make seat available. Current status: {Status}. Only unavailable seats can be made available.");
        
        Status = StatusAvailable;
    }

    public bool IsValidForCreation()
    {
        return !string.IsNullOrWhiteSpace(SectionCode) &&
               RowNumber > 0 &&
               SeatNumber > 0 &&
               Price > 0 &&
               IsValidStatus(Status);
    }

    public void ValidateBusinessRules()
    {
        if (string.IsNullOrWhiteSpace(SectionCode))
            throw new ArgumentException("Section code cannot be empty", nameof(SectionCode));
        
        if (RowNumber <= 0)
            throw new ArgumentException("Row number must be greater than zero", nameof(RowNumber));
        
        if (SeatNumber <= 0)
            throw new ArgumentException("Seat number must be greater than zero", nameof(SeatNumber));
        
        if (Price <= 0)
            throw new ArgumentException("Price must be greater than zero", nameof(Price));
        
        if (!IsValidStatus(Status))
            throw new ArgumentException($"Invalid status: {Status}", nameof(Status));
    }

    private static bool IsValidStatus(string status)
    {
        return status.Equals(StatusAvailable, StringComparison.OrdinalIgnoreCase) ||
               status.Equals(StatusReserved, StringComparison.OrdinalIgnoreCase) ||
               status.Equals(StatusSold, StringComparison.OrdinalIgnoreCase) ||
               status.Equals(StatusUnavailable, StringComparison.OrdinalIgnoreCase);
    }
}
