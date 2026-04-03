using System.Text.Json;
using Inventory.Application.Ports;
using Inventory.Domain.Entities;
using Inventory.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace Inventory.Infrastructure.Workers;

/// <summary>
/// Background worker that polls for expired reservations and publishes `reservation-expired` events.
/// Exposed `ProcessExpiredReservationsAsync` to allow unit tests to run the logic once.
/// </summary>
public class ReservationExpiryWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IKafkaProducer _producer;
    private readonly TimeSpan _pollInterval;

    public ReservationExpiryWorker(IServiceScopeFactory scopeFactory, IKafkaProducer producer)
        : this(scopeFactory, producer, TimeSpan.FromMinutes(1)) { }

    // constructor with configurable poll interval (used by tests)
    public ReservationExpiryWorker(IServiceScopeFactory scopeFactory, IKafkaProducer producer, TimeSpan pollInterval)
    {
        _scopeFactory = scopeFactory ?? throw new ArgumentNullException(nameof(scopeFactory));
        _producer = producer ?? throw new ArgumentNullException(nameof(producer));
        _pollInterval = pollInterval;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessExpiredReservationsAsync(stoppingToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ReservationExpiryWorker error: {ex.Message}");
            }

            await Task.Delay(_pollInterval, stoppingToken).ConfigureAwait(false);
        }
    }

    public async Task ProcessExpiredReservationsAsync(CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<InventoryDbContext>();

        var now = DateTime.UtcNow;
        var expirables = await db.Reservations
            .Where(r => r.Status == Reservation.StatusActive && r.ExpiresAt <= now)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        if (!expirables.Any()) return;

        foreach (var res in expirables)
        {
            res.Status = Reservation.StatusExpired;

            var seat = await db.Seats.FindAsync(new object[] { res.SeatId }, cancellationToken).ConfigureAwait(false);
            if (seat != null)
            {
                seat.Release();
                db.Seats.Update(seat);
            }

            db.Reservations.Update(res);

            // publish event
            var @event = new
            {
                eventId = Guid.NewGuid().ToString("D"),
                reservationId = res.Id.ToString("D"),
                seatId = res.SeatId.ToString("D"),
                customerId = res.CustomerId
            };

            var json = JsonSerializer.Serialize(@event);
            try
            {
                await _producer.ProduceAsync("reservation-expired", json, res.SeatId.ToString("N")).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to publish reservation-expired for {res.Id}: {ex.Message}");
            }
        }

        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}
