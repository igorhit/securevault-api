using FluentResults;
using MediatR;
using SecureVault.Application.Common.DTOs;

namespace SecureVault.Application.Vault.Queries.SearchCredentials;

public record SearchCredentialsQuery(Guid UserId, string SearchTerm) : IRequest<Result<IEnumerable<CredentialResponse>>>;
