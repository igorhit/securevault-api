using FluentAssertions;
using NSubstitute;
using SecureVault.Application.Vault.Commands.CreateCredential;
using SecureVault.Domain.Entities;
using SecureVault.Domain.Interfaces;

namespace SecureVault.UnitTests.Vault;

public class CreateCredentialHandlerTests
{
    private readonly ICredentialRepository _credentialRepository = Substitute.For<ICredentialRepository>();
    private readonly IEncryptionService _encryptionService = Substitute.For<IEncryptionService>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private CreateCredentialCommandHandler CreateHandler() => new(
        _credentialRepository, _encryptionService, _unitOfWork);

    [Fact]
    public async Task Handle_ShouldEncryptSensitiveFieldsBeforeStoring()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var command = new CreateCredentialCommand(userId, "GitHub", "myuser", "mypassword", "https://github.com", null);

        _encryptionService.Encrypt("myuser").Returns("encrypted_user");
        _encryptionService.Encrypt("mypassword").Returns("encrypted_pass");

        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();

        // Verifica que a credencial salva tem os dados CRIPTOGRAFADOS
        await _credentialRepository.Received(1).AddAsync(
            Arg.Is<Credential>(c =>
                c.EncryptedUsername == "encrypted_user" &&
                c.EncryptedPassword == "encrypted_pass" &&
                c.UserId == userId),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ShouldReturnDecryptedDataInResponse()
    {
        // Arrange
        var command = new CreateCredentialCommand(Guid.NewGuid(), "GitHub", "myuser", "mypassword", null, null);
        _encryptionService.Encrypt(Arg.Any<string>()).Returns(x => $"enc_{x.ArgAt<string>(0)}");

        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert — a resposta retorna o plaintext, não o ciphertext
        result.Value.Username.Should().Be("myuser");
        result.Value.Password.Should().Be("mypassword");
    }
}
