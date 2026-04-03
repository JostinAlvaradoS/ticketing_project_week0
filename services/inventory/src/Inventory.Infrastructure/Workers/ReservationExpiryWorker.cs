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
/// ADR-03: Before releasing a seat, checks if a waitlist queue is active for the event.
/// If active, publishes the event WITHOUT releasing the seat (Waitlist Service takes over).
/// Falls back to releasing the seat if the Waitlist Service is unreachable.
/// Exposed `ProcessExpiredReservationsAsync` to allow unit tests to run the logic once.
/// </summary>
public class ReservationExpiryWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IKafkaProducer _producer;
    private readonly IWaitlistClient? _waitlistClient;
    private readonly TimeSpan _pollInterval;

    public ReservationExpiryWorker(IServiceScopeFactory scopeFactory, IKafkaProducer producer, IWaitlistClient? waitlistClient = null)
        : this(scopeFactory, producer, waitlistClient, TimeSpan.FromMinutes(1)) { }

    // Constructor with configurable poll interval (used by tests)
    public ReservationExpiryWorker(IServiceScopeFactory scopeFactory, IKafkaProducer producer, IWaitlistClient? waitlistClient, TimeSpan pollInterval)
    {
        _scopeFactory = scopeFactory ?? throw new ArgumentNullException(nameof(scopeFactory));
        _producer = producer ?? throw new ArgumentNullException(nameof(producer));
        _waitlistClient = waitlistClient;
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

            // ADR-03: only release the seat if no waitlist queue is active for this event.
            // Fallback: if the Waitlist Service is unreachable, release the seat (original behavior).
            var shouldReleaseSeat = await ShouldReleaseSeatAsync(res.EventId, cancellationToken).ConfigureAwait(false);

            var seat = await db.Seats.FindAsync(new object[] { res.SeatId }, cancellationToken).ConfigureAwait(false);
            if (seat != null && shouldReleaseSeat)
            {
                seat.Release();
                db.Seats.Update(seat);
            }

            db.Reservations.Update(res);

            // publish event (v3 payload: messageId = Kafka message UUID, concertEventId = concert/event FK)
            var @event = new
            {
                messageId = Guid.NewGuid().ToString("D"),
                reservationId = res.Id.ToString("D"),
                seatId = res.SeatId.ToString("D"),
                customerId = res.CustomerId,
                concertEventId = res.EventId.ToString("D")
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

    private async Task<bool> ShouldReleaseSeatAsync(Guid eventId, CancellationToken cancellationToken)
    {
        if (_waitlistClient is null) return true;

        try
        {
            var hasPending = await _waitlistClient.HasPendingAsync(eventId, cancellationToken).ConfigureAwait(false);
            return !hasPending;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"ReservationExpiryWorker: waitlist check failed for event {eventId}, falling back to seat release: {ex.Message}");
            return true; // fallback: release the seat
        }
    }
}
