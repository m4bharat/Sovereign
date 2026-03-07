using FluentValidation;
using Sovereign.Application.DTOs;

namespace Sovereign.API.Validators;

public sealed class RewriteMessageRequestValidator : AbstractValidator<RewriteMessageRequest>
{
    public RewriteMessageRequestValidator()
    {
        RuleFor(x => x.UserId).NotEmpty().MaximumLength(128);
        RuleFor(x => x.ContactId).NotEmpty().MaximumLength(128);
        RuleFor(x => x.Draft).NotEmpty().MaximumLength(2000);
        RuleFor(x => x.RelationshipRole).MaximumLength(64);
        RuleFor(x => x.Goal).MaximumLength(64);
        RuleFor(x => x.Platform).MaximumLength(64);
    }
}
