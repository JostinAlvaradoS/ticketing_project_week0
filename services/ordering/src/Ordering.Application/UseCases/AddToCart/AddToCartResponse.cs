using Ordering.Application.DTOs;

namespace Ordering.Application.UseCases.AddToCart;

public record AddToCartResponse(
    bool Success,
    string? ErrorMessage,
    OrderDto? Order
);