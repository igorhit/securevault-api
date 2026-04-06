using FluentResults;
using MediatR;
using SecureVault.Application.Common.DTOs;

namespace SecureVault.Application.Vault.Commands.UpdateCredential;

public record UpdateCredentialCommand(
    Guid CredentialId,
    Guid UserId,
    string Title,
    string Username,
    string Password,
    string? Url,
    string? Notes
) : IRequest<Result<CredentialResponse>>;
