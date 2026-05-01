using FluentValidation;
using QuestionnairesSystem.Application.Features.Questionnaires.Surveys.DTOs;

namespace QuestionnairesSystem.Application.Features.Questionnaires.Surveys.Validators;

public sealed class RejectSurveyRequestValidator : AbstractValidator<RejectSurveyRequest>
{
    public RejectSurveyRequestValidator()
    {
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(2000);
    }
}
