using CrudService.Data;
using CrudService.Models.DTOs;
using CrudService.Models.Entities;
using CrudService.Repositories;

namespace CrudService.Services;

/// <summary>
/// Servicio para gestionar tickets
/// </summary>
public interface ITicketService
{
    Task<IEnumerable<TicketDto>> GetTicketsByEventAsync(long eventId);
    Task<TicketDto?> GetTicketByIdAsync(long id);
    Task<IEnumerable<TicketDto>> CreateTicketsAsync(long eventId, int quantity);
    Task<TicketDto> UpdateTicketStatusAsync(long id, string newStatus, string? reason = null);
    Task<TicketDto> ReleaseTicketAsync(long id, string? reason = null);
    Task<IEnumerable<TicketDto>> GetExpiredTicketsAsync();
}

/// <summary>
/// Implementación del servicio de tickets
/// </summary>
public class TicketService : ITicketService
{
    private readonly ITicketRepository _ticketRepository;
    private readonly ITicketHistoryRepository _historyRepository;
    private readonly IEventRepository _eventRepository;
    private readonly TicketingDbContext _dbContext;
    private readonly ILogger<TicketService> _logger;

    public TicketService(
        ITicketRepository ticketRepository,
        ITicketHistoryRepository historyRepository,
        IEventRepository eventRepository,
        TicketingDbContext dbContext,
        ILogger<TicketService> logger)
    {
        _ticketRepository = ticketRepository;
        _historyRepository = historyRepository;
        _eventRepository = eventRepository;
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<IEnumerable<TicketDto>> GetTicketsByEventAsync(long eventId)
    {
        var tickets = await _ticketRepository.GetByEventIdAsync(eventId);
        return tickets.Select(MapToDto);
    }

    public async Task<TicketDto?> GetTicketByIdAsync(long id)
    {
        var ticket = await _ticketRepository.GetByIdAsync(id);
        return ticket == null ? null : MapToDto(ticket);
    }

    /// <summary>
    /// Crea tickets en lote usando bulk insert
    /// MEJORA CRIT-001: Antes hacia N llamadas a BD, ahora hace 1 sola
    /// MEJORA MED-001: Validar que el evento existe antes de crear tickets
    /// </summary>
    public async Task<IEnumerable<TicketDto>> CreateTicketsAsync(long eventId, int quantity)
    {
        // MED-001: Validar que el evento existe antes de crear tickets
        var eventExists = await _eventRepository.GetByIdAsync(eventId);
        if (eventExists == null)
        {
            throw new KeyNotFoundException($"Evento {eventId} no encontrado. No se pueden crear tickets para un evento inexistente.");
        }

        // Crear todos los tickets en memoria primero
        var tickets = Enumerable.Range(0, quantity)
            .Select(_ => new Ticket
            {
                EventId = eventId,
                Status = TicketStatus.Available
            })
            .ToList();

        // Insertar todos en una sola operacion de BD (bulk insert)
        var created = await _ticketRepository.AddRangeAsync(tickets);

        _logger.LogInformation("Se crearon {Quantity} tickets para evento {EventId} (bulk insert)", quantity, eventId);
        return created.Select(MapToDto);
    }

    /// <summary>
    /// MED-002: Usar transaccion para garantizar atomicidad
    /// </summary>
    public async Task<TicketDto> UpdateTicketStatusAsync(long id, string newStatus, string? reason = null)
    {
        var ticket = await _ticketRepository.GetByIdAsync(id);
        if (ticket == null)
            throw new KeyNotFoundException($"Ticket {id} no encontrado");

        var oldStatus = ticket.Status;

        // Convertir string a enum
        if (!Enum.TryParse<TicketStatus>(newStatus, ignoreCase: true, out var status))
            throw new ArgumentException($"Estado invalido: {newStatus}");

        ticket.Status = status;
        ticket.Version++;

        // Registrar en historial
        var history = new TicketHistory
        {
            TicketId = ticket.Id,
            OldStatus = oldStatus,
            NewStatus = status,
            Reason = reason
        };

        // MED-002: Envolver en transaccion para garantizar atomicidad
        using var transaction = await _dbContext.Database.BeginTransactionAsync();
        try
        {
            await _historyRepository.AddAsync(history);
            var updated = await _ticketRepository.UpdateAsync(ticket);
            await transaction.CommitAsync();

            _logger.LogInformation(
                "Ticket {TicketId} cambio de {OldStatus} a {NewStatus}",
                id, oldStatus, status);

            return MapToDto(updated);
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    /// <summary>
    /// MED-002: Usar transaccion para garantizar atomicidad
    /// </summary>
    public async Task<TicketDto> ReleaseTicketAsync(long id, string? reason = null)
    {
        var ticket = await _ticketRepository.GetByIdAsync(id);
        if (ticket == null)
            throw new KeyNotFoundException($"Ticket {id} no encontrado");

        var oldStatus = ticket.Status;
        ticket.Status = TicketStatus.Available;
        ticket.ReservedAt = null;
        ticket.ExpiresAt = null;
        ticket.ReservedBy = null;
        ticket.OrderId = null;
        ticket.Version++;

        var history = new TicketHistory
        {
            TicketId = ticket.Id,
            OldStatus = oldStatus,
            NewStatus = TicketStatus.Available,
            Reason = reason ?? "Ticket liberado"
        };

        // MED-002: Envolver en transaccion para garantizar atomicidad
        using var transaction = await _dbContext.Database.BeginTransactionAsync();
        try
        {
            await _historyRepository.AddAsync(history);
            var updated = await _ticketRepository.UpdateAsync(ticket);
            await transaction.CommitAsync();

            _logger.LogInformation("Ticket {TicketId} liberado. Razon: {Reason}", id, reason ?? "No especificada");
            return MapToDto(updated);
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task<IEnumerable<TicketDto>> GetExpiredTicketsAsync()
    {
        var expiredTickets = await _ticketRepository.GetExpiredAsync(DateTime.UtcNow);
        return expiredTickets.Select(MapToDto);
    }

    private TicketDto MapToDto(Ticket ticket)
    {
        return new TicketDto
        {
            Id = ticket.Id,
            EventId = ticket.EventId,
            Status = ticket.Status.ToString(),
            ReservedAt = ticket.ReservedAt,
            ExpiresAt = ticket.ExpiresAt,
            PaidAt = ticket.PaidAt,
            OrderId = ticket.OrderId,
            ReservedBy = ticket.ReservedBy,
            Version = ticket.Version
        };
    }
}
