using CrudService.Application.Dtos;
using CrudService.Application.Ports.Inbound;
using CrudService.Application.Ports.Outbound;
using CrudService.Domain.Entities;
using CrudService.Domain.Enums;

namespace CrudService.Application.UseCases.Commands;

public class EventCommands : IEventCommands
{
    private readonly IEventRepository _eventRepository;

    public EventCommands(IEventRepository eventRepository)
    {
        _eventRepository = eventRepository;
    }

    public async Task<EventDto> CreateEventAsync(CreateEventRequest request)
    {
        var @event = new Event
        {
            Name = request.Name,
            StartsAt = request.StartsAt
        };

        var created = await _eventRepository.AddAsync(@event);
        return MapToDto(created);
    }

    public async Task<EventDto> UpdateEventAsync(long id, UpdateEventRequest request)
    {
        var @event = await _eventRepository.GetByIdAsync(id);
        if (@event == null)
            throw new KeyNotFoundException($"Evento {id} no encontrado");

        if (!string.IsNullOrEmpty(request.Name))
            @event.Name = request.Name;

        if (request.StartsAt.HasValue)
            @event.StartsAt = request.StartsAt.Value;

        var updated = await _eventRepository.UpdateAsync(@event);
        return MapToDto(updated);
    }

    public async Task<bool> DeleteEventAsync(long id)
    {
        return await _eventRepository.DeleteAsync(id);
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
