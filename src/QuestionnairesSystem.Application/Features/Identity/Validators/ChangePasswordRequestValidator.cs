using FluentValidation;
using QuestionnairesSystem.Application.Features.Identity.DTOs;

namespace QuestionnairesSystem.Application.Features.Identity.Validators;

public sealed class ChangePasswordRequestValidator : AbstractValidator<ChangePasswordRequest>
{
    public ChangePasswordRequestValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(320);
        RuleFor(x => x.CurrentPassword).NotEmpty().MaximumLength(256);
        RuleFor(x => x.NewPassword).NotEmpty().MinimumLength(8).MaximumLength(256)
            .NotEqual(x => x.CurrentPassword).WithMessage("New password must be different from current password.");
    }
}
