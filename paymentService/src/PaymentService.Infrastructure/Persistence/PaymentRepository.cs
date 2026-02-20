using Microsoft.EntityFrameworkCore;
using PaymentService.Application.Ports.Outbound;
using PaymentService.Domain.Entities;
using PaymentService.Infrastructure.Persistence;

namespace PaymentService.Infrastructure.Persistence;

public class PaymentRepository : IPaymentRepository
{
    private readonly PaymentDbContext _dbContext;

    public PaymentRepository(PaymentDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Payment?> GetByTicketIdAsync(long ticketId)
    {
        return await _dbContext.Payments
            .FirstOrDefaultAsync(p => p.TicketId == ticketId);
    }

    public async Task<Payment?> GetByIdAsync(long id)
    {
        return await _dbContext.Payments
            .Include(p => p.Ticket)
            .FirstOrDefaultAsync(p => p.Id == id);
    }

    public async Task AddAsync(Payment payment, CancellationToken ct)
    {
        payment.CreatedAt = DateTime.UtcNow;
        payment.UpdatedAt = DateTime.UtcNow;
        await _dbContext.Payments.AddAsync(payment, ct);
    }

    public async Task<Payment?> GetByProviderRefAsync(string providerRef, CancellationToken ct)
    {
        return await _dbContext.Payments
            .FirstOrDefaultAsync(p => p.ProviderRef == providerRef, ct);
    }
}
