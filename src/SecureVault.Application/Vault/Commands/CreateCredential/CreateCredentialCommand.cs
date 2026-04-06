using FluentResults;
using MediatR;
using SecureVault.Application.Common.DTOs;

namespace SecureVault.Application.Vault.Commands.CreateCredential;

public record CreateCredentialCommand(
    Guid UserId,
    string Title,
    string Username,
    string Password,
    string? Url,
    string? Notes
) : IRequest<Result<CredentialResponse>>;
