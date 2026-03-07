using FluentValidation;
using Sovereign.Application.DTOs;

namespace Sovereign.API.Validators;

public sealed class UpsertSocialEdgeRequestValidator : AbstractValidator<UpsertSocialEdgeRequest>
{
    public UpsertSocialEdgeRequestValidator()
    {
        RuleFor(x => x.SourceUserId).NotEmpty().MaximumLength(128);
        RuleFor(x => x.TargetContactId).NotEmpty().MaximumLength(128);
        RuleFor(x => x.InteractionCount).GreaterThanOrEqualTo(0);
        RuleFor(x => x.ReciprocityScore).InclusiveBetween(0, 1);
        RuleFor(x => x.MomentumScore).GreaterThanOrEqualTo(0);
        RuleFor(x => x.SilenceDays).GreaterThanOrEqualTo(0);
    }
}
