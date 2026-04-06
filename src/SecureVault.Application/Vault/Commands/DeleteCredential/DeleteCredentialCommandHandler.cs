using FluentResults;
using MediatR;
using SecureVault.Domain.Errors;
using SecureVault.Domain.Interfaces;

namespace SecureVault.Application.Vault.Commands.DeleteCredential;

public class DeleteCredentialCommandHandler : IRequestHandler<DeleteCredentialCommand, Result>
{
    private readonly ICredentialRepository _credentialRepository;
    private readonly IUnitOfWork _unitOfWork;

    public DeleteCredentialCommandHandler(ICredentialRepository credentialRepository, IUnitOfWork unitOfWork)
    {
        _credentialRepository = credentialRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(DeleteCredentialCommand request, CancellationToken cancellationToken)
    {
        var credential = await _credentialRepository.GetByIdAndUserIdAsync(request.CredentialId, request.UserId, cancellationToken);
        if (credential is null)
            return Result.Fail(new Error(DomainErrors.Credential.NotFound));

        await _credentialRepository.DeleteAsync(credential, cancellationToken);
        await _unitOfWork.CommitAsync(cancellationToken);

        return Result.Ok();
    }
}
