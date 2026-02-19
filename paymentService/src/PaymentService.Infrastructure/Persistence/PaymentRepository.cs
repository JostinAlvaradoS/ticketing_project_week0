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

    public async Task<bool> UpdateAsync(Payment payment)
    {
        _dbContext.Payments.Update(payment);
        try
        {
            await _dbContext.SaveChangesAsync();
            return true;
        }
        catch (DbUpdateConcurrencyException)
        {
            return false;
        }
    }

    public async Task<Payment> CreateAsync(Payment payment)
    {
        _dbContext.Payments.Add(payment);
        await _dbContext.SaveChangesAsync();
        return payment;
    }
}
