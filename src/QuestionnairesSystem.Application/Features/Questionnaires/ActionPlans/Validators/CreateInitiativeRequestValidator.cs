using FluentValidation;
using QuestionnairesSystem.Application.Features.Questionnaires.ActionPlans.DTOs;

namespace QuestionnairesSystem.Application.Features.Questionnaires.ActionPlans.Validators;

public sealed class CreateInitiativeRequestValidator : AbstractValidator<CreateInitiativeRequest>
{
    public CreateInitiativeRequestValidator()
    {
        RuleFor(x => x.TitleAr).NotEmpty().MaximumLength(500);
        RuleFor(x => x.TitleEn).NotEmpty().MaximumLength(500);
        RuleFor(x => x.DescriptionAr).MaximumLength(4000);
        RuleFor(x => x.DescriptionEn).MaximumLength(4000);
    }
}
