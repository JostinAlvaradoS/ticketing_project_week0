namespace Identity.Application.UseCases.IssueToken;

public record TokenResult(
    string AccessToken,
    DateTime ExpiresAt
);