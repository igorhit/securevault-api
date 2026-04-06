using FluentResults;
using MediatR;
using SecureVault.Application.Common.DTOs;
using SecureVault.Domain.Interfaces;

namespace SecureVault.Application.Vault.Queries.SearchCredentials;

public class SearchCredentialsQueryHandler : IRequestHandler<SearchCredentialsQuery, Result<IEnumerable<CredentialResponse>>>
{
    private readonly ICredentialRepository _credentialRepository;
    private readonly IEncryptionService _encryptionService;

    public SearchCredentialsQueryHandler(ICredentialRepository credentialRepository, IEncryptionService encryptionService)
    {
        _credentialRepository = credentialRepository;
        _encryptionService = encryptionService;
    }

    public async Task<Result<IEnumerable<CredentialResponse>>> Handle(SearchCredentialsQuery request, CancellationToken cancellationToken)
    {
        // Busca apenas por title e url (campos não criptografados)
        var credentials = await _credentialRepository.SearchByUserIdAsync(request.UserId, request.SearchTerm, cancellationToken);

        var response = credentials.Select(c => new CredentialResponse(
            c.Id,
            c.Title,
            c.Url,
            _encryptionService.Decrypt(c.EncryptedUsername),
            _encryptionService.Decrypt(c.EncryptedPassword),
            c.EncryptedNotes is not null ? _encryptionService.Decrypt(c.EncryptedNotes) : null,
            c.CreatedAt,
            c.UpdatedAt
        ));

        return Result.Ok(response);
    }
}
