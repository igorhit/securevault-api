using FluentResults;
using MediatR;

namespace SecureVault.Application.Vault.Commands.DeleteCredential;

public record DeleteCredentialCommand(Guid CredentialId, Guid UserId) : IRequest<Result>;
