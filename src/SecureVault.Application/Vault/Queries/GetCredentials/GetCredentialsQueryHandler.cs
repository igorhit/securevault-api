using FluentResults;
using MediatR;
using SecureVault.Application.Common.DTOs;
using SecureVault.Domain.Interfaces;

namespace SecureVault.Application.Vault.Queries.GetCredentials;

public class GetCredentialsQueryHandler : IRequestHandler<GetCredentialsQuery, Result<IEnumerable<CredentialResponse>>>
{
    private readonly ICredentialRepository _credentialRepository;
    private readonly IEncryptionService _encryptionService;

    public GetCredentialsQueryHandler(ICredentialRepository credentialRepository, IEncryptionService encryptionService)
    {
        _credentialRepository = credentialRepository;
        _encryptionService = encryptionService;
    }

    public async Task<Result<IEnumerable<CredentialResponse>>> Handle(GetCredentialsQuery request, CancellationToken cancellationToken)
    {
        var credentials = await _credentialRepository.GetByUserIdAsync(request.UserId, cancellationToken);

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
