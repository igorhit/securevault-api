namespace SecureVault.Application.Common.DTOs;

public record CredentialResponse(
    Guid Id,
    string Title,
    string? Url,
    string Username,
    string Password,
    string? Notes,
    DateTime CreatedAt,
    DateTime? UpdatedAt
);
