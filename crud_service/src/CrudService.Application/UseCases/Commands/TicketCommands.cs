using CrudService.Application.Dtos;
using CrudService.Application.Ports.Inbound;
using CrudService.Application.Ports.Outbound;
using CrudService.Domain.Entities;
using CrudService.Domain.Enums;

namespace CrudService.Application.UseCases.Commands;

public class TicketCommands : ITicketCommands
{
    private readonly ITicketRepository _ticketRepository;
    private readonly ITicketHistoryRepository _historyRepository;

    public TicketCommands(ITicketRepository ticketRepository, ITicketHistoryRepository historyRepository)
    {
        _ticketRepository = ticketRepository;
        _historyRepository = historyRepository;
    }

    public async Task<IEnumerable<TicketDto>> CreateTicketsAsync(long eventId, int quantity)
    {
        var tickets = new List<Ticket>();

        for (int i = 0; i < quantity; i++)
        {
            var ticket = new Ticket
            {
                EventId = eventId,
                Status = TicketStatus.Available
            };

            var created = await _ticketRepository.AddAsync(ticket);
            tickets.Add(created);
        }

        return tickets.Select(MapToDto);
    }

    public async Task<TicketDto> UpdateTicketStatusAsync(long id, string newStatus, string? reason = null)
    {
        var ticket = await _ticketRepository.GetByIdAsync(id);
        if (ticket == null)
            throw new KeyNotFoundException($"Ticket {id} no encontrado");

        var oldStatus = ticket.Status;

        if (!Enum.TryParse<TicketStatus>(newStatus, ignoreCase: true, out var status))
            throw new ArgumentException($"Estado inválido: {newStatus}");

        ticket.Status = status;
        ticket.Version++;

        var history = new TicketHistory
        {
            TicketId = ticket.Id,
            OldStatus = oldStatus,
            NewStatus = status,
            Reason = reason
        };

        await _historyRepository.AddAsync(history);
        var updated = await _ticketRepository.UpdateAsync(ticket);

        return MapToDto(updated);
    }

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

        await _historyRepository.AddAsync(history);
        var updated = await _ticketRepository.UpdateAsync(ticket);

        return MapToDto(updated);
    }

    private static TicketDto MapToDto(Ticket ticket)
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
