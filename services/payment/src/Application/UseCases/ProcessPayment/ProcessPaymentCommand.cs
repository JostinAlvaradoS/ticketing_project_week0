using MediatR;
using Payment.Application.DTOs;

namespace Payment.Application.UseCases.ProcessPayment;

public record ProcessPaymentCommand(
    Guid OrderId,
    Guid CustomerId,
    Guid? ReservationId,
    decimal Amount,
    string Currency = "USD",
    string PaymentMethod = "credit_card"
) : IRequest<PaymentResponse>;