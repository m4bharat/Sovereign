using FluentValidation;
using Sovereign.Application.DTOs;

namespace Sovereign.API.Validators;

public sealed class RegisterRequestValidator : AbstractValidator<RegisterRequest>
{
    public RegisterRequestValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Password).NotEmpty().MinimumLength(8);
        RuleFor(x => x.TenantId).NotEmpty().MaximumLength(64);
    }
}
