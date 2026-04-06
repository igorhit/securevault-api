using FluentResults;
using MediatR;
using SecureVault.Application.Common.DTOs;
using SecureVault.Domain.Errors;
using SecureVault.Domain.Interfaces;

namespace SecureVault.Application.Vault.Queries.GetCredentialById;

public class GetCredentialByIdQueryHandler : IRequestHandler<GetCredentialByIdQuery, Result<CredentialResponse>>
{
    private readonly ICredentialRepository _credentialRepository;
    private readonly IEncryptionService _encryptionService;

    public GetCredentialByIdQueryHandler(ICredentialRepository credentialRepository, IEncryptionService encryptionService)
    {
        _credentialRepository = credentialRepository;
        _encryptionService = encryptionService;
    }

    public async Task<Result<CredentialResponse>> Handle(GetCredentialByIdQuery request, CancellationToken cancellationToken)
    {
        var credential = await _credentialRepository.GetByIdAndUserIdAsync(request.CredentialId, request.UserId, cancellationToken);
        if (credential is null)
            return Result.Fail(new Error(DomainErrors.Credential.NotFound));

        return Result.Ok(new CredentialResponse(
            credential.Id,
            credential.Title,
            credential.Url,
            _encryptionService.Decrypt(credential.EncryptedUsername),
            _encryptionService.Decrypt(credential.EncryptedPassword),
            credential.EncryptedNotes is not null ? _encryptionService.Decrypt(credential.EncryptedNotes) : null,
            credential.CreatedAt,
            credential.UpdatedAt
        ));
    }
}
