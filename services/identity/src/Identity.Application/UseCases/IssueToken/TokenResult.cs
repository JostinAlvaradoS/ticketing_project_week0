using Identity.Domain.ValueObjects;

namespace Identity.Application.UseCases.IssueToken;

public record TokenResult(
    string AccessToken,
    DateTime ExpiresAt,
    Role UserRole,
    string UserEmail
);