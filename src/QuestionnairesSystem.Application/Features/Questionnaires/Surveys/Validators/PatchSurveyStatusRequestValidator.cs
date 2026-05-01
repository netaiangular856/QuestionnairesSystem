using FluentValidation;
using QuestionnairesSystem.Application.Features.Questionnaires.Surveys.DTOs;

namespace QuestionnairesSystem.Application.Features.Questionnaires.Surveys.Validators;

public sealed class PatchSurveyStatusRequestValidator : AbstractValidator<PatchSurveyStatusRequest>
{
    public PatchSurveyStatusRequestValidator()
    {
        RuleFor(x => x.Status).IsInEnum();
    }
}
