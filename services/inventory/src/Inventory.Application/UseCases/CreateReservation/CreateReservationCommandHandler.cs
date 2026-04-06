using System.Text.Json;
using MediatR;
using Inventory.Application.DTOs;
using Inventory.Application.Ports;
using Inventory.Domain.Entities;

namespace Inventory.Application.UseCases.CreateReservation;

/// <summary>
/// Handler para crear una reserva de asiento con distributed locking y concurrencia optimista.
/// </summary>
public class CreateReservationCommandHandler : IRequestHandler<CreateReservationCommand, CreateReservationResponse>
{
    private readonly ISeatRepository _seatRepository;
    private readonly IReservationRepository _reservationRepository;
    private readonly IRedisLock _redisLock;
    private readonly IKafkaProducer _kafkaProducer;

    private const string RedisLockKeyPrefix = "lock:seat:";
    private const int LockExpirySeconds = 30;
    private const int ReservationTTLMinutes = 1;

    public CreateReservationCommandHandler(
        ISeatRepository seatRepository,
        IReservationRepository reservationRepository,
        IRedisLock redisLock,
        IKafkaProducer kafkaProducer)
    {
        _seatRepository = seatRepository ?? throw new ArgumentNullException(nameof(seatRepository));
        _reservationRepository = reservationRepository ?? throw new ArgumentNullException(nameof(reservationRepository));
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
            var seat = await _seatRepository.GetByIdAsync(request.SeatId, cancellationToken)
                .ConfigureAwait(false);

            if (seat is null)
            {
                throw new KeyNotFoundException($"Seat {request.SeatId} not found");
            }

            // Domain method encapsula la validación y el cambio de estado
            seat.Reserve();

            var reservation = Reservation.Create(request.SeatId, request.CustomerId, eventId: request.EventId, ttlMinutes: ReservationTTLMinutes);

            await _seatRepository.UpdateAsync(seat, cancellationToken).ConfigureAwait(false);
            var createdReservation = await _reservationRepository.CreateAsync(reservation, cancellationToken).ConfigureAwait(false);

            await PublishReservationCreatedEvent(createdReservation, seat, cancellationToken).ConfigureAwait(false);

            return new CreateReservationResponse(
                ReservationId: createdReservation.Id,
                SeatId: createdReservation.SeatId,
                CustomerId: createdReservation.CustomerId,
                ExpiresAt: createdReservation.ExpiresAt,
                Status: createdReservation.Status
            );
        }
        finally
        {
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

        var jsonOptions = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
        var json = JsonSerializer.Serialize(@event, jsonOptions);
        await _kafkaProducer.ProduceAsync("reservation-created", json, reservation.SeatId.ToString("N"))
            .ConfigureAwait(false);
    }
}
