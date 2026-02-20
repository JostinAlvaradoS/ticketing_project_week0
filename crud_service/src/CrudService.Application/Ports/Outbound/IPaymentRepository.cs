using CrudService.Domain.Entities;

namespace CrudService.Application.Ports.Outbound;

public interface IPaymentRepository
{
    Task<Payment?> GetByIdAsync(long id);
    Task<Payment> AddAsync(Payment payment);
    Task<Payment> UpdateAsync(Payment payment);
    Task<IEnumerable<Payment>> GetByTicketIdAsync(long ticketId);
    Task SaveChangesAsync();
}
