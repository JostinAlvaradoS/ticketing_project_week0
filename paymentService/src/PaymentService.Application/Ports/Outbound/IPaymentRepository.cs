using PaymentService.Domain.Entities;

namespace PaymentService.Application.Ports.Outbound;

public interface IPaymentRepository
{
    Task<Payment?> GetByTicketIdAsync(long ticketId);
    Task<Payment?> GetByIdAsync(long id);
    Task AddAsync(Payment payment, CancellationToken ct);
    Task<Payment?> GetByProviderRefAsync(string providerRef, CancellationToken ct);
}
