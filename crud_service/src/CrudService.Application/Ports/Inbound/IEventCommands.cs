using CrudService.Application.Dtos;

namespace CrudService.Application.Ports.Inbound;

public interface IEventCommands
{
    Task<EventDto> CreateEventAsync(CreateEventRequest request);
    Task<EventDto> UpdateEventAsync(long id, UpdateEventRequest request);
    Task<bool> DeleteEventAsync(long id);
}
