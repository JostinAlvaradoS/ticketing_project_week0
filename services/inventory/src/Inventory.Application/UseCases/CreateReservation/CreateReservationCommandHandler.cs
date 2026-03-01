using System.Text.Json;
using MediatR;
using Inventory.Application.DTOs;
using Inventory.Domain.Entities;
using Inventory.Domain.Ports;
using Inventory.Infrastructure.Persistence;

namespace Inventory.Application.UseCases.CreateReservation;

/// <summary>
/// Handler for creating a seat reservation with distributed locking and optimistic concurrency.
/// </summary>
public class CreateReservationCommandHandler : IRequestHandler<CreateReservationCommand, CreateReservationResponse>
{
    private readonly InventoryDbContext _context;
    private readonly IRedisLock _redisLock;
    private readonly IKafkaProducer _kafkaProducer;

    private const string RedisLockKeyPrefix = "lock:seat:";
    private const int LockExpirySeconds = 30;
    private const int ReservationTTLMinutes = 15;

    public CreateReservationCommandHandler(
        InventoryDbContext context,
        IRedisLock redisLock,
        IKafkaProducer kafkaProducer)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _redisLock = redisLock ?? throw new ArgumentNullException(nameof(redisLock));
        _kafkaProducer = kafkaProducer ?? throw new ArgumentNullException(nameof(kafkaProducer));
    }

    public async Task<CreateReservationResponse> Handle(CreateReservationCommand request, CancellationToken cancellationToken)
    {
        if (request.SeatId == Guid.Empty) throw new ArgumentException("SeatId cannot be empty", nameof(request));
        if (string.IsNullOrEmpty(request.CustomerId)) throw new ArgumentException("CustomerId cannot be empty", nameof(request));

        // HUMAN CHECK: Se utiliza Redis para asegurar exclusión mutua en la reserva del asiento.
        // Se prefiere un lock distribuido sobre un lock de DB para reducir la carga en PostgreSQL
        // y permitir una mayor concurrencia en el escalado de servicios.
        var lockKey = $"{RedisLockKeyPrefix}{request.SeatId:N}";
        var lockToken = await _redisLock.AcquireLockAsync(lockKey, TimeSpan.FromSeconds(LockExpirySeconds))
            .ConfigureAwait(false);

        if (lockToken is null)
        {
            throw new InvalidOperationException($"Could not acquire lock for seat {request.SeatId}. Seat may be reserved or locked.");
        }

        try
        {
            // Fetch the seat and check if already reserved
            var seat = await _context.Seats.FindAsync([request.SeatId], cancellationToken: cancellationToken)
                .ConfigureAwait(false);

            if (seat is null)
            {
                throw new KeyNotFoundException($"Seat {request.SeatId} not found");
            }

            if (seat.Reserved)
            {
                throw new InvalidOperationException($"Seat {request.SeatId} is already reserved");
            }

            // Create reservation with 15-minute TTL
            var now = DateTime.UtcNow;
            var expiresAt = now.AddMinutes(ReservationTTLMinutes);
            var reservation = new Reservation
            {
                Id = Guid.NewGuid(),
                SeatId = request.SeatId,
                CustomerId = request.CustomerId,
                CreatedAt = now,
                ExpiresAt = expiresAt,
                Status = "active"
            };

            // Update seat: mark as reserved
            seat.Reserved = true;

            // Add reservation and update seat in transaction
            _context.Reservations.Add(reservation);
            _context.Seats.Update(seat);
            await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            // Publish reservation-created event
            await PublishReservationCreatedEvent(reservation, seat, cancellationToken).ConfigureAwait(false);

            return new CreateReservationResponse(
                ReservationId: reservation.Id,
                SeatId: reservation.SeatId,
                CustomerId: reservation.CustomerId,
                ExpiresAt: reservation.ExpiresAt,
                Status: reservation.Status
            );
        }
        finally
        {
            // Always release the lock
            await _redisLock.ReleaseLockAsync(lockKey, lockToken).ConfigureAwait(false);
        }
    }

    private async Task PublishReservationCreatedEvent(Reservation reservation, Seat seat, CancellationToken cancellationToken)
    {
        var @event = new ReservationCreatedEvent(
            EventId: Guid.NewGuid().ToString("D"),
            ReservationId: reservation.Id.ToString("D"),
            CustomerId: reservation.CustomerId,
            SeatId: reservation.SeatId.ToString("D"),
            SeatNumber: $"{seat.Section}-{seat.Row}-{seat.Number}",
            Section: seat.Section,
            BasePrice: 0m, // TODO: fetch from catalog service or seat pricing model
            CreatedAt: reservation.CreatedAt.ToString("O"),
            ExpiresAt: reservation.ExpiresAt.ToString("O"),
            Status: reservation.Status
        );

        var jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
        var json = JsonSerializer.Serialize(@event, jsonOptions);
        await _kafkaProducer.ProduceAsync("reservation-created", json, reservation.SeatId.ToString("N"))
            .ConfigureAwait(false);
    }
}
