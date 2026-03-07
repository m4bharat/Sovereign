using FluentValidation;
using Sovereign.Application.DTOs;

namespace Sovereign.API.Validators;

public sealed class CreateRelationshipRequestValidator : AbstractValidator<CreateRelationshipRequest>
{
    public CreateRelationshipRequestValidator()
    {
        RuleFor(x => x.UserId).NotEmpty().MaximumLength(128);
        RuleFor(x => x.ContactId).NotEmpty().MaximumLength(128);
        RuleFor(x => x.Role).IsInEnum();
    }
}
