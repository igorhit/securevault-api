using FluentResults;
using MediatR;
using SecureVault.Application.Common.DTOs;
using SecureVault.Domain.Entities;
using SecureVault.Domain.Interfaces;

namespace SecureVault.Application.Vault.Commands.CreateCredential;

public class CreateCredentialCommandHandler : IRequestHandler<CreateCredentialCommand, Result<CredentialResponse>>
{
    private readonly ICredentialRepository _credentialRepository;
    private readonly IEncryptionService _encryptionService;
    private readonly IUnitOfWork _unitOfWork;

    public CreateCredentialCommandHandler(
        ICredentialRepository credentialRepository,
        IEncryptionService encryptionService,
        IUnitOfWork unitOfWork)
    {
        _credentialRepository = credentialRepository;
        _encryptionService = encryptionService;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<CredentialResponse>> Handle(CreateCredentialCommand request, CancellationToken cancellationToken)
    {
        var encryptedUsername = _encryptionService.Encrypt(request.Username);
        var encryptedPassword = _encryptionService.Encrypt(request.Password);
        var encryptedNotes = request.Notes is not null ? _encryptionService.Encrypt(request.Notes) : null;

        var credential = Credential.Create(
            request.UserId,
            request.Title,
            encryptedUsername,
            encryptedPassword,
            request.Url,
            encryptedNotes
        );

        await _credentialRepository.AddAsync(credential, cancellationToken);
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
