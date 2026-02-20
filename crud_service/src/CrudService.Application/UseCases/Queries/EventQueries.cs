using CrudService.Application.Dtos;
using CrudService.Application.Ports.Inbound;
using CrudService.Application.Ports.Outbound;
using CrudService.Domain.Entities;
using CrudService.Domain.Enums;

namespace CrudService.Application.UseCases.Queries;

public class EventQueries : IEventQueries
{
    private readonly IEventRepository _eventRepository;

    public EventQueries(IEventRepository eventRepository)
    {
        _eventRepository = eventRepository;
    }

    public async Task<IEnumerable<EventDto>> GetAllEventsAsync()
    {
        var events = await _eventRepository.GetAllAsync();
        return events.Select(MapToDto);
    }

    public async Task<EventDto?> GetEventByIdAsync(long id)
    {
        var @event = await _eventRepository.GetByIdAsync(id);
        return @event == null ? null : MapToDto(@event);
    }

    private static EventDto MapToDto(Event @event)
    {
        var tickets = @event.Tickets;
        return new EventDto
        {
            Id = @event.Id,
            Name = @event.Name,
            StartsAt = @event.StartsAt,
            AvailableTickets = tickets.Count(t => t.Status == TicketStatus.Available),
            ReservedTickets = tickets.Count(t => t.Status == TicketStatus.Reserved),
            PaidTickets = tickets.Count(t => t.Status == TicketStatus.Paid)
        };
    }
}
