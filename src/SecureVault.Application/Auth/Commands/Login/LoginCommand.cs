using FluentResults;
using MediatR;
using SecureVault.Application.Common.DTOs;

namespace SecureVault.Application.Auth.Commands.Login;

public record LoginCommand(string Email, string Password) : IRequest<Result<AuthResponse>>;
