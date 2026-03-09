namespace Payment.Application.DTOs;

public record PaymentRequest(
    Guid OrderId,
    Guid CustomerId,
    Guid? ReservationId,
    decimal Amount,
    string Currency = "USD",
    string PaymentMethod = "credit_card"
);

public record PaymentResponse(
    bool Success,
    string? ErrorMessage,
    PaymentDto? Payment
);

public record PaymentDto(
    Guid Id,
    Guid OrderId,
    Guid CustomerId,
    Guid? ReservationId,
    decimal Amount,
    string Currency,
    string PaymentMethod,
    string Status,
    string? ErrorCode,
    string? ErrorMessage,
    string? FailureReason,
    DateTime CreatedAt,
    DateTime? ProcessedAt,
    bool IsSimulated,
    string? SimulatedResponse
);