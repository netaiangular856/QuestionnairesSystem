using FluentValidation;
using QuestionnairesSystem.Application.Features.Questionnaires.ActionPlans.DTOs;

namespace QuestionnairesSystem.Application.Features.Questionnaires.ActionPlans.Validators;

public sealed class UpdateActionPlanRequestValidator : AbstractValidator<UpdateActionPlanRequest>
{
    public UpdateActionPlanRequestValidator()
    {
        RuleFor(x => x.TitleAr).NotEmpty().MaximumLength(500);
        RuleFor(x => x.TitleEn).NotEmpty().MaximumLength(500);
        RuleFor(x => x.DescriptionAr).MaximumLength(4000);
        RuleFor(x => x.DescriptionEn).MaximumLength(4000);
        RuleFor(x => x.Status).IsInEnum();
        RuleFor(x => x)
            .Must(x => !x.StartDateUtc.HasValue || !x.EndDateUtc.HasValue || x.EndDateUtc >= x.StartDateUtc)
            .WithMessage("End date must be on or after start date.");
    }
}
