using FluentResults;
using MediatR;
using SecureVault.Application.Common.DTOs;
using SecureVault.Domain.Entities;
using SecureVault.Domain.Errors;
using SecureVault.Domain.Interfaces;

namespace SecureVault.Application.Auth.Commands.Refresh;

public class RefreshCommandHandler : IRequestHandler<RefreshCommand, Result<AuthResponse>>
{
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly IUserRepository _userRepository;
    private readonly ITokenService _tokenService;
    private readonly IUnitOfWork _unitOfWork;

    public RefreshCommandHandler(
        IRefreshTokenRepository refreshTokenRepository,
        IUserRepository userRepository,
        ITokenService tokenService,
        IUnitOfWork unitOfWork)
    {
        _refreshTokenRepository = refreshTokenRepository;
        _userRepository = userRepository;
        _tokenService = tokenService;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<AuthResponse>> Handle(RefreshCommand request, CancellationToken cancellationToken)
    {
        var storedToken = await _refreshTokenRepository.GetByTokenAsync(request.RefreshToken, cancellationToken);

        if (storedToken is null)
            return Result.Fail(new Error(DomainErrors.Token.Invalid));

        if (storedToken.IsRevoked)
            return Result.Fail(new Error(DomainErrors.Token.Revoked));

        if (!storedToken.IsActive)
            return Result.Fail(new Error(DomainErrors.Token.Expired));

        var user = await _userRepository.GetByIdAsync(storedToken.UserId, cancellationToken);
        if (user is null)
            return Result.Fail(new Error(DomainErrors.User.NotFound));

        // Rotação de refresh token: invalida o atual e emite um novo
        storedToken.Revoke();

        var newAccessToken = _tokenService.GenerateAccessToken(user);
        var newRawRefreshToken = _tokenService.GenerateRefreshToken();
        var newRefreshToken = RefreshToken.Create(user.Id, newRawRefreshToken, DateTime.UtcNow.AddDays(7));

        await _refreshTokenRepository.AddAsync(newRefreshToken, cancellationToken);
        await _unitOfWork.CommitAsync(cancellationToken);

        return Result.Ok(new AuthResponse(
            newAccessToken,
            newRawRefreshToken,
            DateTime.UtcNow.AddMinutes(15)
        ));
    }
}
