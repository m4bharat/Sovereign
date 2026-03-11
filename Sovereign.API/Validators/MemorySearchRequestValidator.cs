using FluentValidation;
using Sovereign.Application.DTOs;

namespace Sovereign.API.Validators;

public sealed class MemorySearchRequestValidator : AbstractValidator<MemorySearchRequest>
{
    public MemorySearchRequestValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.Query).NotEmpty();
        RuleFor(x => x.Limit).InclusiveBetween(1, 20);
    }
}
