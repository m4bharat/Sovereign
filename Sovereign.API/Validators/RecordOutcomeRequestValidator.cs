using FluentValidation;
using Sovereign.Application.DTOs;

namespace Sovereign.API.Validators;

public sealed class RecordOutcomeRequestValidator : AbstractValidator<RecordOutcomeRequest>
{
    public RecordOutcomeRequestValidator()
    {
        RuleFor(x => x.OutcomeLabel).NotEmpty();
    }
}
