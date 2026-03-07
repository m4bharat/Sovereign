using FluentValidation;
using Sovereign.Application.DTOs;

namespace Sovereign.API.Validators;

public sealed class AiDecisionRequestValidator : AbstractValidator<AiDecisionRequest>
{
    public AiDecisionRequestValidator()
    {
        RuleFor(x => x.UserId).NotEmpty().MaximumLength(128);
        RuleFor(x => x.ContactId).NotEmpty().MaximumLength(128);
        RuleFor(x => x.Message).NotEmpty().MaximumLength(4000);
        RuleFor(x => x.RelationshipRole).MaximumLength(64);
        RuleFor(x => x.RecentSummary).MaximumLength(4000);
        RuleFor(x => x.LastTopicSummary).MaximumLength(4000);
    }
}
