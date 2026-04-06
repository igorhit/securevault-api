using FluentResults;
using MediatR;
using SecureVault.Application.Common.DTOs;

namespace SecureVault.Application.Vault.Queries.GetCredentialById;

public record GetCredentialByIdQuery(Guid CredentialId, Guid UserId) : IRequest<Result<CredentialResponse>>;
