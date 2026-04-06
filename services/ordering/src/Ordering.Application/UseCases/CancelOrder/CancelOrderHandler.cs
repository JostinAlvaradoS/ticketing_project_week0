using MediatR;
using Ordering.Application.Ports;

namespace Ordering.Application.UseCases.CancelOrder;

public sealed class CancelOrderHandler : IRequestHandler<CancelOrderCommand, bool>
{
    private readonly IOrderRepository _orderRepository;

    public CancelOrderHandler(IOrderRepository orderRepository)
    {
        _orderRepository = orderRepository;
    }

    public async Task<bool> Handle(CancelOrderCommand request, CancellationToken cancellationToken)
    {
        var order = await _orderRepository.GetByIdAsync(request.OrderId, cancellationToken);

        if (order is null)
            return false;

        try
        {
            order.Cancel();
        }
        catch (InvalidOperationException)
        {
            return false;
        }

        await _orderRepository.UpdateAsync(order, cancellationToken);
        return true;
    }
}
