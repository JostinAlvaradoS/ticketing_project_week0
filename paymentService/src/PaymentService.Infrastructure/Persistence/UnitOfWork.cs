using System.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using PaymentService.Application.Exceptions;
using PaymentService.Application.Ports.Outbound;

namespace PaymentService.Infrastructure.Persistence;

public class UnitOfWork : IUnitOfWork
{
    private readonly PaymentDbContext _context;

    public UnitOfWork(PaymentDbContext context)
        => _context = context;

    public async Task<int> SaveChangesAsync(CancellationToken ct = default)
    {
        try
        {
            return await _context.SaveChangesAsync(ct);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            throw new ConcurrencyException("Concurrency conflict detected", ex);
        }
        catch (DbUpdateException ex) when (IsDuplicateKeyException(ex))
        {
            throw new DuplicateEntryException("Duplicate entry detected", ex);
        }
    }

    public async Task<ITransaction> BeginTransactionAsync(
        IsolationLevel isolationLevel = IsolationLevel.ReadCommitted,
        CancellationToken ct = default)
    {
        var tx = await _context.Database.BeginTransactionAsync(isolationLevel, ct);
        return new TransactionAdapter(tx);
    }

    private static bool IsDuplicateKeyException(DbUpdateException ex)
    {
        return ex.InnerException?.Message.Contains("duplicate key", StringComparison.OrdinalIgnoreCase) == true
            || ex.InnerException?.Message.Contains("unique constraint", StringComparison.OrdinalIgnoreCase) == true;
    }
}

/// <summary>
/// Adapter que envuelve IDbContextTransaction de EF Core
/// y expone la interfaz ITransaction de Application.
/// </summary>
internal class TransactionAdapter : ITransaction
{
    private readonly IDbContextTransaction _inner;

    public TransactionAdapter(IDbContextTransaction inner)
        => _inner = inner;

    public Task CommitAsync(CancellationToken cancellationToken = default)
        => _inner.CommitAsync(cancellationToken);

    public Task RollbackAsync(CancellationToken cancellationToken = default)
        => _inner.RollbackAsync(cancellationToken);

    public ValueTask DisposeAsync()
        => _inner.DisposeAsync();
}