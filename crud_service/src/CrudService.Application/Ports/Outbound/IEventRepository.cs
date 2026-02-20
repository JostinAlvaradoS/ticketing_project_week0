using CrudService.Domain.Entities;

namespace CrudService.Application.Ports.Outbound;

public interface IEventRepository
{
    Task<IEnumerable<Event>> GetAllAsync();
    Task<Event?> GetByIdAsync(long id);
    Task<Event> AddAsync(Event @event);
    Task<Event> UpdateAsync(Event @event);
    Task<bool> DeleteAsync(long id);
    Task SaveChangesAsync();
}
