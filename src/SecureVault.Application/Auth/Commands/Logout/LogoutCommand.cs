using FluentResults;
using MediatR;

namespace SecureVault.Application.Auth.Commands.Logout;

public record LogoutCommand(Guid UserId) : IRequest<Result>;
