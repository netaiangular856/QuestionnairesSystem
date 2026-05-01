using FluentValidation;
using QuestionnairesSystem.Application.Features.Identity.DTOs;
namespace QuestionnairesSystem.Application.Features.Identity.Validators;

public sealed class CreateUserRequestValidator : AbstractValidator<CreateUserRequest>
{
    public CreateUserRequestValidator()
    {
        RuleFor(x => x.UserName).NotEmpty().MaximumLength(128);
        RuleFor(x => x.NameAr).MaximumLength(128);
        RuleFor(x => x.NameEn).MaximumLength(128);
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(320);
        RuleFor(x => x.Password).NotEmpty().MinimumLength(8).MaximumLength(256);
    }
}

