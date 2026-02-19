using CrudService.Application.Dtos;

namespace CrudService.Application.Ports.Inbound;

public interface IEventQueries
{
    Task<IEnumerable<EventDto>> GetAllEventsAsync();
    Task<EventDto?> GetEventByIdAsync(long id);
}
