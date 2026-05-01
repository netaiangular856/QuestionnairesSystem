using FluentValidation;
using QuestionnairesSystem.Application.Features.Identity.DTOs;

namespace QuestionnairesSystem.Application.Features.Identity.Validators;

public sealed class UpdateUserRequestValidator : AbstractValidator<UpdateUserRequest>
{
    public UpdateUserRequestValidator()
    {
        RuleFor(x => x.UserName).NotEmpty().MaximumLength(128);
        RuleFor(x => x.NameAr).MaximumLength(128);
        RuleFor(x => x.NameEn).MaximumLength(128);
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(320);
        RuleFor(x => x.NewPassword)
            .MaximumLength(256)
            .MinimumLength(8)
            .When(x => !string.IsNullOrWhiteSpace(x.NewPassword));
    }
}

