using FluentResults;
using MediatR;
using SecureVault.Application.Common.DTOs;

namespace SecureVault.Application.Auth.Commands.Register;

public record RegisterCommand(string Email, string Password) : IRequest<Result<AuthResponse>>;
