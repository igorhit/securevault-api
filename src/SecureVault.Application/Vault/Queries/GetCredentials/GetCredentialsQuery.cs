using FluentResults;
using MediatR;
using SecureVault.Application.Common.DTOs;

namespace SecureVault.Application.Vault.Queries.GetCredentials;

public record GetCredentialsQuery(Guid UserId) : IRequest<Result<IEnumerable<CredentialResponse>>>;
