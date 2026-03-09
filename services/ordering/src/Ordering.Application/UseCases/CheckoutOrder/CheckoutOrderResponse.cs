using Ordering.Application.DTOs;

namespace Ordering.Application.UseCases.CheckoutOrder;

public record CheckoutOrderResponse(
    bool Success,
    string? ErrorMessage,
    OrderDto? Order
);