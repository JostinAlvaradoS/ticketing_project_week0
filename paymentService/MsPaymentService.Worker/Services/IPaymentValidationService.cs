using PaymentService.Api.Models.DTOs;
using PaymentService.Api.Models.Events;

namespace PaymentService.Api.Services;

public interface IPaymentValidationService
{
    Task<ValidationResult> ValidateAndProcessApprovedPaymentAsync(PaymentApprovedEvent paymentEvent);
    Task<ValidationResult> ValidateAndProcessRejectedPaymentAsync(PaymentRejectedEvent paymentEvent);
    bool IsWithinTimeLimit(DateTime reservedAt, DateTime paymentReceivedAt);
}