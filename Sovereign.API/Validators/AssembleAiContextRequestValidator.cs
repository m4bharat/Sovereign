using FluentValidation;
using Sovereign.Application.DTOs;

namespace Sovereign.API.Validators;

public sealed class AssembleAiContextRequestValidator : AbstractValidator<AssembleAiContextRequest>
{
    public AssembleAiContextRequestValidator()
    {
        RuleFor(x => x.UserId).NotEmpty().MaximumLength(128);
        RuleFor(x => x.ContactId).NotEmpty().MaximumLength(128);
        RuleFor(x => x.Message).NotEmpty().MaximumLength(4000);
        RuleFor(x => x.RelationshipRole).MaximumLength(64);
    }
}
