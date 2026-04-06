using FluentAssertions;
using NSubstitute;
using SecureVault.Application.Auth.Commands.Login;
using SecureVault.Domain.Entities;
using SecureVault.Domain.Errors;
using SecureVault.Domain.Interfaces;

namespace SecureVault.UnitTests.Auth;

public class LoginCommandHandlerTests
{
    private readonly IUserRepository _userRepository = Substitute.For<IUserRepository>();
    private readonly IRefreshTokenRepository _refreshTokenRepository = Substitute.For<IRefreshTokenRepository>();
    private readonly IPasswordHasher _passwordHasher = Substitute.For<IPasswordHasher>();
    private readonly ITokenService _tokenService = Substitute.For<ITokenService>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private LoginCommandHandler CreateHandler() => new(
        _userRepository, _refreshTokenRepository, _passwordHasher, _tokenService, _unitOfWork);

    [Fact]
    public async Task Handle_WithValidCredentials_ShouldReturnTokens()
    {
        // Arrange
        var user = User.Create("test@example.com", "hashed_password");
        var command = new LoginCommand("test@example.com", "P@ssw0rd!");

        _userRepository.GetByEmailAsync(command.Email).Returns(user);
        _passwordHasher.Verify(command.Password, user.PasswordHash).Returns(true);
        _tokenService.GenerateAccessToken(user).Returns("access_token");
        _tokenService.GenerateRefreshToken().Returns("refresh_token");

        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.AccessToken.Should().Be("access_token");
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithInvalidPassword_ShouldReturnFailure()
    {
        // Arrange
        var user = User.Create("test@example.com", "hashed_password");
        var command = new LoginCommand("test@example.com", "WrongP@ssw0rd");

        _userRepository.GetByEmailAsync(command.Email).Returns(user);
        _passwordHasher.Verify(command.Password, user.PasswordHash).Returns(false);

        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailed.Should().BeTrue();
        result.Errors.Should().ContainSingle(e => e.Message == DomainErrors.User.InvalidCredentials);
    }

    [Fact]
    public async Task Handle_WithNonexistentEmail_ShouldReturnFailure()
    {
        // Arrange
        var command = new LoginCommand("nobody@example.com", "P@ssw0rd!");
        _userRepository.GetByEmailAsync(command.Email).Returns((User?)null);

        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailed.Should().BeTrue();
        // Não revela se o email existe: mesmo erro para email inválido e senha inválida
        result.Errors.Should().ContainSingle(e => e.Message == DomainErrors.User.InvalidCredentials);
    }
}
