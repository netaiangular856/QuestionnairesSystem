using FluentValidation;
using QuestionnairesSystem.Application.Features.Questionnaires.ActionPlans.DTOs;

namespace QuestionnairesSystem.Application.Features.Questionnaires.ActionPlans.Validators;

public sealed class AddInitiativeProgressRequestValidator : AbstractValidator<AddInitiativeProgressRequest>
{
    public AddInitiativeProgressRequestValidator()
    {
        RuleFor(x => x.ProgressPercent).InclusiveBetween(0, 100).When(x => x.ProgressPercent.HasValue);
        RuleFor(x => x.Notes).MaximumLength(4000);
    }
}
