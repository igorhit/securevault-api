using FluentValidation;

namespace SecureVault.Application.Vault.Commands.CreateCredential;

public class CreateCredentialCommandValidator : AbstractValidator<CreateCredentialCommand>
{
    public CreateCredentialCommandValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Username).NotEmpty().MaximumLength(254);
        RuleFor(x => x.Password).NotEmpty().MaximumLength(1000);
        RuleFor(x => x.Url).MaximumLength(2048).When(x => x.Url is not null);
        RuleFor(x => x.Notes).MaximumLength(5000).When(x => x.Notes is not null);
    }
}
