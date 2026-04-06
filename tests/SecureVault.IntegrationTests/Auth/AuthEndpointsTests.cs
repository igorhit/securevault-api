using FluentAssertions;
using SecureVault.Application.Common.DTOs;
using System.Net;
using System.Net.Http.Json;

namespace SecureVault.IntegrationTests.Auth;

public class AuthEndpointsTests : IClassFixture<IntegrationTestFactory>
{
    private readonly HttpClient _client;

    public AuthEndpointsTests(IntegrationTestFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Register_WithValidData_ShouldReturn201AndTokens()
    {
        // Arrange
        var request = new { Email = $"user_{Guid.NewGuid()}@test.com", Password = "Secure@123" };

        // Act
        var response = await _client.PostAsJsonAsync("/auth/register", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await response.Content.ReadFromJsonAsync<AuthResponse>();
        body!.AccessToken.Should().NotBeNullOrEmpty();
        body.RefreshToken.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task Register_WithDuplicateEmail_ShouldReturn409()
    {
        // Arrange
        var email = $"dup_{Guid.NewGuid()}@test.com";
        var request = new { Email = email, Password = "Secure@123" };

        await _client.PostAsJsonAsync("/auth/register", request);

        // Act
        var response = await _client.PostAsJsonAsync("/auth/register", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Register_WithWeakPassword_ShouldReturn400()
    {
        // Arrange
        var request = new { Email = "test@test.com", Password = "weak" };

        // Act
        var response = await _client.PostAsJsonAsync("/auth/register", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Login_WithValidCredentials_ShouldReturn200AndTokens()
    {
        // Arrange
        var email = $"login_{Guid.NewGuid()}@test.com";
        var password = "Secure@123";

        await _client.PostAsJsonAsync("/auth/register", new { Email = email, Password = password });

        // Act
        var response = await _client.PostAsJsonAsync("/auth/login", new { Email = email, Password = password });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<AuthResponse>();
        body!.AccessToken.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task Login_WithWrongPassword_ShouldReturn401()
    {
        // Arrange
        var email = $"login2_{Guid.NewGuid()}@test.com";
        await _client.PostAsJsonAsync("/auth/register", new { Email = email, Password = "Secure@123" });

        // Act
        var response = await _client.PostAsJsonAsync("/auth/login", new { Email = email, Password = "WrongPass@1" });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Refresh_WithValidToken_ShouldReturn200AndNewTokens()
    {
        // Arrange
        var email = $"refresh_{Guid.NewGuid()}@test.com";
        var registerResponse = await _client.PostAsJsonAsync("/auth/register", new { Email = email, Password = "Secure@123" });
        var auth = await registerResponse.Content.ReadFromJsonAsync<AuthResponse>();

        // Act
        var response = await _client.PostAsJsonAsync("/auth/refresh", new { RefreshToken = auth!.RefreshToken });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var newAuth = await response.Content.ReadFromJsonAsync<AuthResponse>();
        newAuth!.RefreshToken.Should().NotBe(auth.RefreshToken); // Token rotacionado
    }
}
