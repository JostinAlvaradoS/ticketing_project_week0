namespace Catalog.Domain.Entities;

public class Event
{
    public Guid Id { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public DateTime EventDate { get; private set; }
    public string Venue { get; private set; } = string.Empty;
    public int MaxCapacity { get; private set; }
    public decimal BasePrice { get; private set; }
    public string Status { get; private set; } = "active"; // active, inactive, cancelled
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }
    
    public ICollection<Seat> Seats { get; private set; } = new List<Seat>();

    // Parameterless constructor for EF Core
    private Event() { }

    // Factory method for creating new events with business validation
    public static Event Create(
        string name,
        string description,
        DateTime eventDate,
        string venue,
        int maxCapacity,
        decimal basePrice)
    {
        ValidateEventCreationData(name, description, eventDate, venue, maxCapacity, basePrice);

        return new Event
        {
            Id = Guid.NewGuid(),
            Name = name,
            Description = description,
            EventDate = eventDate,
            Venue = venue,
            MaxCapacity = maxCapacity,
            BasePrice = basePrice,
            Status = "active",
            CreatedAt = DateTime.UtcNow,
            Seats = new List<Seat>()
        };
    }

    // Business validation for event creation following Gherkin scenarios
    private static void ValidateEventCreationData(
        string name,
        string description,
        DateTime eventDate,
        string venue,
        int maxCapacity,
        decimal basePrice)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("El nombre del evento es obligatorio", nameof(name));

        if (string.IsNullOrWhiteSpace(description))
            throw new ArgumentException("La descripción del evento es obligatoria", nameof(description));

        if (eventDate <= DateTime.UtcNow)
            throw new ArgumentException("La fecha del evento debe ser futura", nameof(eventDate));

        if (string.IsNullOrWhiteSpace(venue))
            throw new ArgumentException("El venue es obligatorio", nameof(venue));

        if (maxCapacity <= 0)
            throw new ArgumentException("La capacidad máxima debe ser mayor a cero", nameof(maxCapacity));

        if (basePrice <= 0)
            throw new ArgumentException("El precio base debe ser mayor a cero", nameof(basePrice));
    }

    // Business Logic Methods
    public bool IsValidForCreation()
    {
        return !string.IsNullOrWhiteSpace(Name) &&
               EventDate > DateTime.UtcNow &&
               BasePrice > 0;
    }

    public bool IsBookable()
    {
        return IsActive &&
               EventDate > DateTime.UtcNow &&
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

    // Update event details with validation
    public void UpdateDetails(string name, string description, int maxCapacity)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Event name is required", nameof(name));

        if (string.IsNullOrWhiteSpace(description))
            throw new ArgumentException("Event description is required", nameof(description));

        if (maxCapacity <= 0)
            throw new ArgumentException("La capacidad máxima debe ser mayor a cero", nameof(maxCapacity));

        // Don't allow reducing capacity below current seat count
        if (maxCapacity < Seats.Count)
            throw new InvalidOperationException("No se puede reducir la capacidad por debajo del número de asientos existentes");

        Name = name;
        Description = description;
        MaxCapacity = maxCapacity;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Deactivate()
    {
        if (HasActiveReservations())
            throw new InvalidOperationException("No se puede desactivar un evento con reservas activas");

        Status = "inactive";
        UpdatedAt = DateTime.UtcNow;
        
        // Make all available seats unavailable
        foreach (var seat in Seats.Where(s => s.IsAvailable()))
        {
            seat.MakeUnavailable();
        }
    }

    public void Reactivate()
    {
        if (EventDate <= DateTime.UtcNow)
            throw new InvalidOperationException("No se puede reactivar un evento que ya pasó");

        Status = "active";
        UpdatedAt = DateTime.UtcNow;
        
        // Make all unavailable seats available again
        foreach (var seat in Seats.Where(s => s.IsUnavailable()))
        {
            seat.MakeAvailable();
        }
    }

    public bool HasActiveReservations()
    {
        return Seats.Any(s => s.IsReserved());
    }

    public bool HasSoldTickets()
    {
        return Seats.Any(s => s.IsSold());
    }

    public void UpdateBasePriceIfAllowed(decimal newBasePrice)
    {
        if (newBasePrice <= 0)
            throw new ArgumentException("El precio base debe ser mayor a cero", nameof(newBasePrice));

        if (HasActiveReservations() || HasSoldTickets())
            throw new InvalidOperationException("No se puede modificar el precio base si ya existen reservas o boletos vendidos");

        BasePrice = newBasePrice;
        UpdatedAt = DateTime.UtcNow;
    }

    public void ValidateSeatCapacity(int seatCount)
    {
        if (seatCount > MaxCapacity)
            throw new InvalidOperationException("La cantidad de asientos excede la capacidad máxima del evento");
    }

    public bool IsActive => Status == "active";
    public bool CanBeModified => Status == "active" && EventDate > DateTime.UtcNow;
}
