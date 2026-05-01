using FluentValidation;
using QuestionnairesSystem.Application.Features.Questionnaires.Surveys.DTOs;
using QuestionnairesSystem.Shared.Constants;

namespace QuestionnairesSystem.Application.Features.Questionnaires.Surveys.Validators;

public sealed class SurveyFilterRequestValidator : AbstractValidator<SurveyFilterRequest>
{
    public SurveyFilterRequestValidator()
    {
        RuleFor(x => x.Page).GreaterThan(0);
        RuleFor(x => x.PageSize).GreaterThan(0).LessThanOrEqualTo(PaginationConstants.MaxPageSize);
    }
}
