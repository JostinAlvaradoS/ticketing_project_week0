namespace Catalog.Domain.Entities;

public class Event
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime EventDate { get; set; }
    public decimal BasePrice { get; set; }
    
    public ICollection<Seat> Seats { get; set; } = new List<Seat>();

    // Business Logic Methods
    public bool IsValidForCreation()
    {
        return !string.IsNullOrWhiteSpace(Name) &&
               EventDate > DateTime.UtcNow &&
               BasePrice > 0;
    }

    public bool IsBookable()
    {
        return EventDate > DateTime.UtcNow &&
               HasAvailableSeats();
    }

    public bool HasAvailableSeats()
    {
        return Seats.Any(s => s.IsAvailable());
    }

    public int GetAvailableSeatsCount()
    {
        return Seats.Count(s => s.IsAvailable());
    }

    public int GetReservedSeatsCount()
    {
        return Seats.Count(s => s.IsReserved());
    }

    public int GetSoldSeatsCount()
    {
        return Seats.Count(s => s.IsSold());
    }

    public IEnumerable<Seat> GetAvailableSeats()
    {
        return Seats.Where(s => s.IsAvailable());
    }

    public IEnumerable<Seat> GetSeatsBySection(string sectionCode)
    {
        return Seats.Where(s => s.SectionCode.Equals(sectionCode, StringComparison.OrdinalIgnoreCase));
    }

    public void ValidateBusinessRules()
    {
        if (string.IsNullOrWhiteSpace(Name))
            throw new ArgumentException("Event name cannot be empty", nameof(Name));
        
        if (EventDate <= DateTime.UtcNow)
            throw new ArgumentException("Event date must be in the future", nameof(EventDate));
        
        if (BasePrice <= 0)
            throw new ArgumentException("Base price must be greater than zero", nameof(BasePrice));
    }
}
