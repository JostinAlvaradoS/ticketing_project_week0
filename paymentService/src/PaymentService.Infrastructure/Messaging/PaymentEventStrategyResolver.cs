using Microsoft.Extensions.DependencyInjection;
using PaymentService.Application.Ports.Inbound;

namespace PaymentService.Infrastructure.Messaging;

/// <summary>
/// Resolves the correct IPaymentEventStrategy based on the event type.
/// Uses IServiceScopeFactory to create scoped instances per resolution,
/// supporting the DI lifecycle of strategies and their dependencies.
/// </summary>
public class PaymentEventStrategyResolver
{
    private readonly IServiceScopeFactory _scopeFactory;

    public PaymentEventStrategyResolver(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }

    /// <summary>
    /// Creates a new scope and resolves the strategy matching the given event type.
    /// The caller is responsible for disposing the returned scope.
    /// </summary>
    public (IPaymentEventStrategy Strategy, IServiceScope Scope) Resolve(string eventType)
    {
        var scope = _scopeFactory.CreateScope();
        var strategies = scope.ServiceProvider.GetServices<IPaymentEventStrategy>();

        var strategy = strategies.FirstOrDefault(s =>
            s.EventType.Equals(eventType, StringComparison.OrdinalIgnoreCase));

        if (strategy is null)
        {
            scope.Dispose();
            throw new InvalidOperationException(
                $"No IPaymentEventStrategy registered for event type '{eventType}'. " +
                $"Ensure a strategy is registered in DI for this event type.");
        }

        return (strategy, scope);
    }
}
