namespace Producer.Application.Ports.Inbound;

using Producer.Application.Dtos;

public interface IProcessPaymentUseCase
{
    Task<ProcessPaymentResult> ExecuteAsync(ProcessPaymentRequest request, CancellationToken cancellationToken = default);
}
