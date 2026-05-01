using FluentValidation;
using QuestionnairesSystem.Application.Features.Identity.DTOs;

namespace QuestionnairesSystem.Application.Features.Identity.Validators;

public sealed class CreateRoleRequestValidator : AbstractValidator<CreateRoleRequest>
{
    public CreateRoleRequestValidator()
    {
        RuleFor(x => x.NameAr).NotEmpty().MaximumLength(128);
        RuleFor(x => x.NameEn).NotEmpty().MaximumLength(128);
        RuleFor(x => x.DescriptionAr).MaximumLength(512);
        RuleFor(x => x.DescriptionEn).MaximumLength(512);
    }
}

