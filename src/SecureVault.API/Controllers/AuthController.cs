using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SecureVault.Application.Auth.Commands.Login;
using SecureVault.Application.Auth.Commands.Logout;
using SecureVault.Application.Auth.Commands.Refresh;
using SecureVault.Application.Auth.Commands.Register;
using SecureVault.Application.Common.DTOs;
using System.Security.Claims;

namespace SecureVault.API.Controllers;

[ApiController]
[Route("auth")]
public class AuthController : ControllerBase
{
    private readonly ISender _sender;

    public AuthController(ISender sender)
    {
        _sender = sender;
    }

    /// <summary>Registra um novo usuário e retorna tokens de acesso.</summary>
    [HttpPost("register")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Register([FromBody] RegisterCommand command, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(command, cancellationToken);

        if (result.IsFailed)
        {
            var error = result.Errors.First();
            if (error.Message == Domain.Errors.DomainErrors.User.EmailAlreadyExists)
                return Conflict(new { error = "E-mail já cadastrado." });

            return BadRequest(new { errors = result.Errors.Select(e => e.Message) });
        }

        return CreatedAtAction(nameof(Register), result.Value);
    }

    /// <summary>Autentica o usuário e retorna tokens de acesso.</summary>
    [HttpPost("login")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login([FromBody] LoginCommand command, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(command, cancellationToken);

        if (result.IsFailed)
            return Unauthorized(new { error = "Credenciais inválidas." }); // Mensagem genérica intencional

        return Ok(result.Value);
    }

    /// <summary>Renova o access token usando um refresh token válido.</summary>
    [HttpPost("refresh")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Refresh([FromBody] RefreshCommand command, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(command, cancellationToken);

        if (result.IsFailed)
            return Unauthorized(new { error = "Token inválido ou expirado." });

        return Ok(result.Value);
    }

    /// <summary>Invalida todos os refresh tokens do usuário autenticado.</summary>
    [HttpPost("logout")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Logout(CancellationToken cancellationToken)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue("sub")
            ?? throw new UnauthorizedAccessException());

        await _sender.Send(new LogoutCommand(userId), cancellationToken);
        return NoContent();
    }
}
