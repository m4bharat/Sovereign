using FluentValidation;
using Sovereign.Application.DTOs;

namespace Sovereign.API.Validators;

public sealed class AddMessageRequestValidator : AbstractValidator<AddMessageRequest>
{
    public AddMessageRequestValidator()
    {
        RuleFor(x => x.ThreadId).NotEmpty();
        RuleFor(x => x.SenderType).NotEmpty().MaximumLength(32);
        RuleFor(x => x.Content).NotEmpty().MaximumLength(4000);
    }
}
