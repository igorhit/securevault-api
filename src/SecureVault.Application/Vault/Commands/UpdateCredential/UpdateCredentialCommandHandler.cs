using FluentResults;
using MediatR;
using SecureVault.Application.Common.DTOs;
using SecureVault.Domain.Errors;
using SecureVault.Domain.Interfaces;

namespace SecureVault.Application.Vault.Commands.UpdateCredential;

public class UpdateCredentialCommandHandler : IRequestHandler<UpdateCredentialCommand, Result<CredentialResponse>>
{
    private readonly ICredentialRepository _credentialRepository;
    private readonly IEncryptionService _encryptionService;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateCredentialCommandHandler(
        ICredentialRepository credentialRepository,
        IEncryptionService encryptionService,
        IUnitOfWork unitOfWork)
    {
        _credentialRepository = credentialRepository;
        _encryptionService = encryptionService;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<CredentialResponse>> Handle(UpdateCredentialCommand request, CancellationToken cancellationToken)
    {
        // Busca filtrando por UserId para garantir que o usuário só acessa suas próprias credenciais
        var credential = await _credentialRepository.GetByIdAndUserIdAsync(request.CredentialId, request.UserId, cancellationToken);
        if (credential is null)
            return Result.Fail(new Error(DomainErrors.Credential.NotFound));

        var encryptedUsername = _encryptionService.Encrypt(request.Username);
        var encryptedPassword = _encryptionService.Encrypt(request.Password);
        var encryptedNotes = request.Notes is not null ? _encryptionService.Encrypt(request.Notes) : null;

        credential.Update(request.Title, encryptedUsername, encryptedPassword, request.Url, encryptedNotes);

        await _credentialRepository.UpdateAsync(credential, cancellationToken);
        await _unitOfWork.CommitAsync(cancellationToken);

        return Result.Ok(new CredentialResponse(
            credential.Id,
            credential.Title,
            credential.Url,
            request.Username,
            request.Password,
            request.Notes,
            credential.CreatedAt,
            credential.UpdatedAt
        ));
    }
}
