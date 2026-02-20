using Producer.Application.Dtos;
using Producer.Application.Ports.Inbound;
using Producer.Application.Ports.Outbound;
using Producer.Domain.Events;

namespace Producer.Application.UseCases;

public class ProcessPaymentUseCase : IProcessPaymentUseCase
{
    private readonly IPaymentEventPublisher _paymentPublisher;

    public ProcessPaymentUseCase(IPaymentEventPublisher paymentPublisher)
    {
        _paymentPublisher = paymentPublisher;
    }

    public async Task<ProcessPaymentResult> ExecuteAsync(ProcessPaymentRequest request, CancellationToken cancellationToken = default)
    {
        if (request.TicketId <= 0)
            return new ProcessPaymentResult { Success = false, Message = "TicketId debe ser mayor a 0" };

        if (request.EventId <= 0)
            return new ProcessPaymentResult { Success = false, Message = "EventId debe ser mayor a 0" };

        if (request.AmountCents <= 0)
            return new ProcessPaymentResult { Success = false, Message = "AmountCents debe ser mayor a 0" };

        if (string.IsNullOrWhiteSpace(request.PaymentBy))
            return new ProcessPaymentResult { Success = false, Message = "PaymentBy es requerido" };

        if (string.IsNullOrWhiteSpace(request.PaymentMethodId))
            return new ProcessPaymentResult { Success = false, Message = "PaymentMethodId es requerido" };

        var isApproved = await SimulatePaymentProcessingAsync(cancellationToken);

        if (isApproved)
        {
            var approvedEvent = new PaymentApprovedEvent
            {
                TicketId = request.TicketId,
                EventId = request.EventId,
                AmountCents = request.AmountCents,
                Currency = request.Currency,
                PaymentBy = request.PaymentBy,
                TransactionRef = request.TransactionRef ?? $"TXN-{Guid.NewGuid()}",
                ApprovedAt = DateTime.UtcNow
            };

            await _paymentPublisher.PublishApprovedAsync(approvedEvent, cancellationToken);

            return new ProcessPaymentResult
            {
                Success = true,
                TicketId = request.TicketId,
                EventId = request.EventId,
                Status = "approved",
                Message = "Pago aprobado"
            };
        }
        else
        {
            var rejectedEvent = new PaymentRejectedEvent
            {
                TicketId = request.TicketId,
                EventId = request.EventId,
                AmountCents = request.AmountCents,
                Currency = request.Currency,
                PaymentBy = request.PaymentBy,
                TransactionRef = request.TransactionRef,
                RejectionReason = "Fondos insuficientes o tarjeta rechazada",
                RejectedAt = DateTime.UtcNow
            };

            await _paymentPublisher.PublishRejectedAsync(rejectedEvent, cancellationToken);

            return new ProcessPaymentResult
            {
                Success = true,
                TicketId = request.TicketId,
                EventId = request.EventId,
                Status = "rejected",
                Message = "Pago rechazado"
            };
        }
    }

    private async Task<bool> SimulatePaymentProcessingAsync(CancellationToken cancellationToken)
    {
        await Task.Delay(Random.Shared.Next(100, 500), cancellationToken);
        return Random.Shared.Next(0, 100) < 80;
    }
}
