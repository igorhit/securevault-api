using FluentAssertions;
using SecureVault.Application.Common.DTOs;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace SecureVault.IntegrationTests.Vault;

public class VaultEndpointsTests : IClassFixture<IntegrationTestFactory>
{
    private readonly HttpClient _client;

    public VaultEndpointsTests(IntegrationTestFactory factory)
    {
        _client = factory.CreateClient();
    }

    private async Task<string> AuthenticateAsync()
    {
        var email = $"vault_{Guid.NewGuid()}@test.com";
        var response = await _client.PostAsJsonAsync("/auth/register", new { Email = email, Password = "Secure@123" });
        var auth = await response.Content.ReadFromJsonAsync<AuthResponse>();
        return auth!.AccessToken;
    }

    [Fact]
    public async Task GetAll_WithoutAuth_ShouldReturn401()
    {
        var response = await _client.GetAsync("/vault");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task CreateAndGet_ShouldPersistCredential()
    {
        // Arrange
        var token = await AuthenticateAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var createRequest = new
        {
            Title = "GitHub",
            Username = "myuser",
            Password = "ghp_secrettoken",
            Url = "https://github.com",
            Notes = (string?)null
        };

        // Act — cria
        var createResponse = await _client.PostAsJsonAsync("/vault", createRequest);
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = await createResponse.Content.ReadFromJsonAsync<CredentialResponse>();

        // Act — busca por ID
        var getResponse = await _client.GetAsync($"/vault/{created!.Id}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var fetched = await getResponse.Content.ReadFromJsonAsync<CredentialResponse>();

        // Assert — dados retornados em plaintext
        fetched!.Title.Should().Be("GitHub");
        fetched.Username.Should().Be("myuser");
        fetched.Password.Should().Be("ghp_secrettoken");
    }

    [Fact]
    public async Task UserCannotAccessAnotherUsersCredential()
    {
        // Arrange — usuário A cria uma credencial
        var tokenA = await AuthenticateAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokenA);

        var createResponse = await _client.PostAsJsonAsync("/vault", new
        {
            Title = "Private", Username = "u", Password = "p", Url = (string?)null, Notes = (string?)null
        });
        var created = await createResponse.Content.ReadFromJsonAsync<CredentialResponse>();

        // Act — usuário B tenta acessar
        var tokenB = await AuthenticateAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokenB);

        var response = await _client.GetAsync($"/vault/{created!.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound); // Não revela que existe (403 revelaria)
    }

    [Fact]
    public async Task Delete_ShouldRemoveCredential()
    {
        // Arrange
        var token = await AuthenticateAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var createResponse = await _client.PostAsJsonAsync("/vault", new
        {
            Title = "ToDelete", Username = "u", Password = "p", Url = (string?)null, Notes = (string?)null
        });
        var created = await createResponse.Content.ReadFromJsonAsync<CredentialResponse>();

        // Act
        var deleteResponse = await _client.DeleteAsync($"/vault/{created!.Id}");
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Assert — não encontra mais
        var getResponse = await _client.GetAsync($"/vault/{created.Id}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
