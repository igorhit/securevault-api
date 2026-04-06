using FluentAssertions;
using NSubstitute;
using SecureVault.Application.Auth.Commands.Register;
using SecureVault.Domain.Entities;
using SecureVault.Domain.Errors;
using SecureVault.Domain.Interfaces;

namespace SecureVault.UnitTests.Auth;

public class RegisterCommandHandlerTests
{
    private readonly IUserRepository _userRepository = Substitute.For<IUserRepository>();
    private readonly IRefreshTokenRepository _refreshTokenRepository = Substitute.For<IRefreshTokenRepository>();
    private readonly IPasswordHasher _passwordHasher = Substitute.For<IPasswordHasher>();
    private readonly ITokenService _tokenService = Substitute.For<ITokenService>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private RegisterCommandHandler CreateHandler() => new(
        _userRepository, _refreshTokenRepository, _passwordHasher, _tokenService, _unitOfWork);

    [Fact]
    public async Task Handle_WhenEmailNotExists_ShouldReturnTokens()
    {
        // Arrange
        var command = new RegisterCommand("test@example.com", "P@ssw0rd!");

        _userRepository.ExistsByEmailAsync(command.Email).Returns(false);
        _passwordHasher.Hash(command.Password).Returns("hashed_password");
        _tokenService.GenerateAccessToken(Arg.Any<User>()).Returns("access_token");
        _tokenService.GenerateRefreshToken().Returns("refresh_token");

        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.AccessToken.Should().Be("access_token");
        result.Value.RefreshToken.Should().Be("refresh_token");
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenEmailAlreadyExists_ShouldReturnFailure()
    {
        // Arrange
        var command = new RegisterCommand("existing@example.com", "P@ssw0rd!");
        _userRepository.ExistsByEmailAsync(command.Email).Returns(true);

        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailed.Should().BeTrue();
        result.Errors.Should().ContainSingle(e => e.Message == DomainErrors.User.EmailAlreadyExists);
        await _unitOfWork.DidNotReceive().CommitAsync(Arg.Any<CancellationToken>());
    }
}
