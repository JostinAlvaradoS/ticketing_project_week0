
using System.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace MsPaymentService.Worker.Data;

public class EfUnitOfWork : IUnitOfWork
{
    private readonly PaymentDbContext _context;

    public EfUnitOfWork(PaymentDbContext context)
        => _context = context;

    public Task<int> SaveChangesAsync(CancellationToken ct = default)
        => _context.SaveChangesAsync(ct);

    public Task<IDbContextTransaction> BeginTransactionAsync(
        IsolationLevel isolationLevel = IsolationLevel.ReadCommitted,
        CancellationToken ct = default)
        => _context.Database.BeginTransactionAsync(isolationLevel, ct);
}