using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SecureVault.Application.Common.DTOs;
using SecureVault.Application.Vault.Commands.CreateCredential;
using SecureVault.Application.Vault.Commands.DeleteCredential;
using SecureVault.Application.Vault.Commands.UpdateCredential;
using SecureVault.Application.Vault.Queries.GetCredentialById;
using SecureVault.Application.Vault.Queries.GetCredentials;
using SecureVault.Application.Vault.Queries.SearchCredentials;
using System.Security.Claims;

namespace SecureVault.API.Controllers;

[ApiController]
[Route("vault")]
[Authorize]
public class VaultController : ControllerBase
{
    private readonly ISender _sender;

    public VaultController(ISender sender)
    {
        _sender = sender;
    }

    private Guid CurrentUserId => Guid.Parse(
        User.FindFirstValue(ClaimTypes.NameIdentifier)
        ?? User.FindFirstValue("sub")
        ?? throw new UnauthorizedAccessException());

    /// <summary>Retorna todas as credenciais do usuário autenticado.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<CredentialResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new GetCredentialsQuery(CurrentUserId), cancellationToken);
        return Ok(result.Value);
    }

    /// <summary>Busca credenciais pelo título ou URL.</summary>
    [HttpGet("search")]
    [ProducesResponseType(typeof(IEnumerable<CredentialResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Search([FromQuery] string q, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new SearchCredentialsQuery(CurrentUserId, q), cancellationToken);
        return Ok(result.Value);
    }

    /// <summary>Retorna uma credencial específica pelo ID.</summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(CredentialResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new GetCredentialByIdQuery(id, CurrentUserId), cancellationToken);

        if (result.IsFailed)
            return NotFound();

        return Ok(result.Value);
    }

    /// <summary>Cria uma nova credencial no cofre do usuário.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(CredentialResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreateCredentialRequest request, CancellationToken cancellationToken)
    {
        var command = new CreateCredentialCommand(CurrentUserId, request.Title, request.Username, request.Password, request.Url, request.Notes);
        var result = await _sender.Send(command, cancellationToken);

        if (result.IsFailed)
            return BadRequest(new { errors = result.Errors.Select(e => e.Message) });

        return CreatedAtAction(nameof(GetById), new { id = result.Value.Id }, result.Value);
    }

    /// <summary>Atualiza uma credencial existente.</summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(CredentialResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Update(Guid id, [FromBody] CreateCredentialRequest request, CancellationToken cancellationToken)
    {
        var command = new UpdateCredentialCommand(id, CurrentUserId, request.Title, request.Username, request.Password, request.Url, request.Notes);
        var result = await _sender.Send(command, cancellationToken);

        if (result.IsFailed)
        {
            if (result.Errors.Any(e => e.Message == Domain.Errors.DomainErrors.Credential.NotFound))
                return NotFound();
            return BadRequest(new { errors = result.Errors.Select(e => e.Message) });
        }

        return Ok(result.Value);
    }

    /// <summary>Remove uma credencial do cofre.</summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new DeleteCredentialCommand(id, CurrentUserId), cancellationToken);

        if (result.IsFailed)
            return NotFound();

        return NoContent();
    }
}

public record CreateCredentialRequest(string Title, string Username, string Password, string? Url, string? Notes);
