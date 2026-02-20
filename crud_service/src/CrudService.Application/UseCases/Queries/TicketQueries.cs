using CrudService.Application.Dtos;
using CrudService.Application.Ports.Inbound;
using CrudService.Application.Ports.Outbound;
using CrudService.Domain.Entities;

namespace CrudService.Application.UseCases.Queries;

public class TicketQueries : ITicketQueries
{
    private readonly ITicketRepository _ticketRepository;

    public TicketQueries(ITicketRepository ticketRepository)
    {
        _ticketRepository = ticketRepository;
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

    public async Task<IEnumerable<TicketDto>> GetExpiredTicketsAsync()
    {
        var expiredTickets = await _ticketRepository.GetExpiredAsync(DateTime.UtcNow);
        return expiredTickets.Select(MapToDto);
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
