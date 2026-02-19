using PaymentService.Domain.Entities;

namespace PaymentService.Application.Ports.Outbound;

public interface IPaymentRepository
{
    Task<Payment?> GetByTicketIdAsync(long ticketId);
    Task<Payment?> GetByIdAsync(long id);
    Task<bool> UpdateAsync(Payment payment);
    Task<Payment> CreateAsync(Payment payment);
}
