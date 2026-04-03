using Microsoft.EntityFrameworkCore;
using Ordering.Application.Ports;
using Ordering.Domain.Entities;

namespace Ordering.Infrastructure.Persistence;

/// <summary>
/// EF Core implementation of the Order repository.
/// </summary>
public class OrderRepository : IOrderRepository
{
    private readonly OrderingDbContext _context;

    public OrderRepository(OrderingDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task<Order?> GetByIdAsync(Guid orderId, CancellationToken cancellationToken = default)
    {
        return await _context.Orders
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == orderId, cancellationToken);
    }

    public async Task<Order?> GetDraftOrderAsync(string? userId, string? guestToken, CancellationToken cancellationToken = default)
    {
        var query = _context.Orders
            .Include(o => o.Items)
            .Where(o => o.State == Order.StateDraft);

        if (!string.IsNullOrEmpty(userId))
        {
            query = query.Where(o => o.UserId == userId);
        }
        else if (!string.IsNullOrEmpty(guestToken))
        {
            query = query.Where(o => o.GuestToken == guestToken);
        }
        else
        {
            return null; // Cannot find draft order without user or guest identification
        }

        return await query.FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<Order> CreateAsync(Order order, CancellationToken cancellationToken = default)
    {
        _context.Orders.Add(order);
        await _context.SaveChangesAsync(cancellationToken);
        
        // Reload with items to return consistent state
        return await GetByIdAsync(order.Id, cancellationToken) 
            ?? throw new InvalidOperationException("Created order not found");
    }

    public async Task<Order> UpdateAsync(Order order, CancellationToken cancellationToken = default)
    {
        _context.Orders.Update(order);
        await _context.SaveChangesAsync(cancellationToken);
        
        // Reload with items to return consistent state
        return await GetByIdAsync(order.Id, cancellationToken)
            ?? throw new InvalidOperationException("Updated order not found");
    }

    public async Task<IEnumerable<Order>> GetOrdersByUserAsync(string userId, CancellationToken cancellationToken = default)
    {
        return await _context.Orders
            .Include(o => o.Items)
            .Where(o => o.UserId == userId)
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Order>> GetOrdersByGuestTokenAsync(string guestToken, CancellationToken cancellationToken = default)
    {
        return await _context.Orders
            .Include(o => o.Items)
            .Where(o => o.GuestToken == guestToken)
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<Order?> GetActiveOrderBySeatIdAsync(Guid seatId, CancellationToken cancellationToken = default)
    {
        return await _context.Orders
            .Include(o => o.Items)
            .Where(o => (o.State == Order.StateDraft || o.State == Order.StatePending)
                        && o.Items.Any(i => i.SeatId == seatId))
            .FirstOrDefaultAsync(cancellationToken);
    }
}