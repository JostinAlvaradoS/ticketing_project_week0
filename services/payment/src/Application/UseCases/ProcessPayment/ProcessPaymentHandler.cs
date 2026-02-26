using MediatR;
using Microsoft.Extensions.Logging;
using Payment.Application.DTOs;
using Payment.Application.Events;
using Payment.Application.Ports;
using System.Text.Json;

namespace Payment.Application.UseCases.ProcessPayment;

public sealed class ProcessPaymentHandler : IRequestHandler<ProcessPaymentCommand, PaymentResponse>
{
    private readonly IPaymentRepository _paymentRepository;
    private readonly IOrderValidationService _orderValidationService;
    private readonly IReservationValidationService _reservationValidationService;
    private readonly IPaymentSimulatorService _paymentSimulatorService;
    private readonly IKafkaProducer _kafkaProducer;
    private readonly ILogger<ProcessPaymentHandler> _logger;

    public ProcessPaymentHandler(
        IPaymentRepository paymentRepository,
        IOrderValidationService orderValidationService,
        IReservationValidationService reservationValidationService,
        IPaymentSimulatorService paymentSimulatorService,
        IKafkaProducer kafkaProducer,
        ILogger<ProcessPaymentHandler> logger)
    {
        _paymentRepository = paymentRepository;
        _orderValidationService = orderValidationService;
        _reservationValidationService = reservationValidationService;
        _paymentSimulatorService = paymentSimulatorService;
        _kafkaProducer = kafkaProducer;
        _logger = logger;
    }

    public async Task<PaymentResponse> Handle(ProcessPaymentCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Processing payment for order {OrderId}, customer {CustomerId}, amount {Amount} {Currency}",
            request.OrderId, request.CustomerId, request.Amount, request.Currency);

        try
        {
            // Step 1: Validate order state
            var orderValidation = await _orderValidationService.ValidateOrderAsync(
                request.OrderId, request.CustomerId, request.Amount, cancellationToken);
            
            if (!orderValidation.IsValid)
            {
                _logger.LogWarning("Order validation failed for order {OrderId}: {Error}", 
                    request.OrderId, orderValidation.ErrorMessage);
                return new PaymentResponse(false, $"Order validation failed: {orderValidation.ErrorMessage}", null);
            }

            // Step 2: Re-check reservation if provided
            if (request.ReservationId.HasValue)
            {
                var reservationValidation = await _reservationValidationService.ValidateReservationAsync(
                    request.ReservationId.Value, request.CustomerId, cancellationToken);
                
                if (!reservationValidation.IsValid)
                {
                    _logger.LogWarning("Reservation validation failed for reservation {ReservationId}: {Error}", 
                        request.ReservationId, reservationValidation.ErrorMessage);
                    return new PaymentResponse(false, $"Reservation validation failed: {reservationValidation.ErrorMessage}", null);
                }
            }

            // Step 3: Create payment record
            var payment = new Domain.Entities.Payment
            {
                Id = Guid.NewGuid(),
                OrderId = request.OrderId,
                CustomerId = request.CustomerId,
                ReservationId = request.ReservationId,
                Amount = request.Amount,
                Currency = request.Currency,
                PaymentMethod = request.PaymentMethod,
                Status = "pending",
                CreatedAt = DateTime.UtcNow,
                IsSimulated = true
            };

            var createdPayment = await _paymentRepository.CreateAsync(payment, cancellationToken);
            _logger.LogDebug("Payment record created with ID {PaymentId}", createdPayment.Id);

            // Step 4: Simulate charge
            var simulationResult = await _paymentSimulatorService.SimulatePaymentAsync(
                request.Amount, request.Currency, request.PaymentMethod, cancellationToken);

            // Step 5: Update payment with results and publish events
            if (simulationResult.Success)
            {
                createdPayment.Status = "succeeded";
                createdPayment.ProcessedAt = DateTime.UtcNow;
                createdPayment.SimulatedResponse = $"Success - TransactionId: {simulationResult.TransactionId}";
                
                var updatedPayment = await _paymentRepository.UpdateAsync(createdPayment, cancellationToken);

                // Publish payment-succeeded event
                try
                {
                    await PublishPaymentSucceededEvent(updatedPayment, simulationResult.TransactionId, cancellationToken);
                    _logger.LogInformation("Payment event published successfully for payment {PaymentId}", updatedPayment.Id);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to publish payment-succeeded event for payment {PaymentId}", updatedPayment.Id);
                }

                _logger.LogInformation("Payment {PaymentId} succeeded for order {OrderId}", 
                    updatedPayment.Id, request.OrderId);

                var paymentDto = MapToDto(updatedPayment);
                return new PaymentResponse(true, null, paymentDto);
            }
            else
            {
                createdPayment.Status = "failed";
                createdPayment.ProcessedAt = DateTime.UtcNow;
                createdPayment.ErrorCode = simulationResult.ErrorCode;
                createdPayment.ErrorMessage = simulationResult.ErrorMessage;
                createdPayment.FailureReason = simulationResult.FailureReason;
                createdPayment.SimulatedResponse = $"Failed - {simulationResult.ErrorCode}: {simulationResult.ErrorMessage}";
                
                var updatedPayment = await _paymentRepository.UpdateAsync(createdPayment, cancellationToken);

                // Publish payment-failed event
                try
                {
                    await PublishPaymentFailedEvent(updatedPayment, cancellationToken);
                    _logger.LogInformation("Payment event published successfully for payment {PaymentId}", updatedPayment.Id);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to publish payment-failed event for payment {PaymentId}", updatedPayment.Id);
                }

                _logger.LogWarning("Payment {PaymentId} failed for order {OrderId}: {Error}", 
                    updatedPayment.Id, request.OrderId, simulationResult.ErrorMessage);

                var paymentDto = MapToDto(updatedPayment);
                return new PaymentResponse(false, $"Payment failed: {simulationResult.ErrorMessage}", paymentDto);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing payment for order {OrderId}", request.OrderId);
            return new PaymentResponse(false, $"Payment processing error: {ex.Message}", null);
        }
    }

    private async Task PublishPaymentSucceededEvent(Domain.Entities.Payment payment, string? transactionId, CancellationToken cancellationToken)
    {
        var paymentEvent = new PaymentSucceededEvent(
            PaymentId: payment.Id.ToString(),
            OrderId: payment.OrderId.ToString(),
            CustomerId: payment.CustomerId.ToString(),
            ReservationId: payment.ReservationId?.ToString(),
            Amount: payment.Amount,
            Currency: payment.Currency,
            PaymentMethod: payment.PaymentMethod,
            TransactionId: transactionId,
            ProcessedAt: payment.ProcessedAt!.Value
        );

        var eventJson = JsonSerializer.Serialize(paymentEvent, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
        _logger.LogInformation("Publishing payment-succeeded event for payment {PaymentId} to topic 'payment-succeeded'", payment.Id);
        await _kafkaProducer.ProduceAsync("payment-succeeded", eventJson, payment.Id.ToString("N"));
        
        _logger.LogInformation("Successfully published payment-succeeded event for payment {PaymentId}", payment.Id);
    }

    private async Task PublishPaymentFailedEvent(Domain.Entities.Payment payment, CancellationToken cancellationToken)
    {
        var paymentEvent = new PaymentFailedEvent(
            PaymentId: payment.Id.ToString(),
            OrderId: payment.OrderId.ToString(),
            CustomerId: payment.CustomerId.ToString(),
            ReservationId: payment.ReservationId?.ToString(),
            Amount: payment.Amount,
            Currency: payment.Currency,
            PaymentMethod: payment.PaymentMethod,
            ErrorCode: payment.ErrorCode,
            ErrorMessage: payment.ErrorMessage,
            FailureReason: payment.FailureReason,
            AttemptedAt: payment.ProcessedAt!.Value
        );

        var eventJson = JsonSerializer.Serialize(paymentEvent, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
        _logger.LogInformation("Publishing payment-failed event for payment {PaymentId} to topic 'payment-failed'", payment.Id);
        await _kafkaProducer.ProduceAsync("payment-failed", eventJson, payment.Id.ToString("N"));
        
        _logger.LogInformation("Successfully published payment-failed event for payment {PaymentId}", payment.Id);
    }

    private static PaymentDto MapToDto(Domain.Entities.Payment payment)
    {
        return new PaymentDto(
            payment.Id,
            payment.OrderId,
            payment.CustomerId,
            payment.ReservationId,
            payment.Amount,
            payment.Currency,
            payment.PaymentMethod,
            payment.Status,
            payment.ErrorCode,
            payment.ErrorMessage,
            payment.FailureReason,
            payment.CreatedAt,
            payment.ProcessedAt,
            payment.IsSimulated,
            payment.SimulatedResponse
        );
    }
}