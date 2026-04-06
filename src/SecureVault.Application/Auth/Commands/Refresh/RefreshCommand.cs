using FluentResults;
using MediatR;
using SecureVault.Application.Common.DTOs;

namespace SecureVault.Application.Auth.Commands.Refresh;

public record RefreshCommand(string RefreshToken) : IRequest<Result<AuthResponse>>;
