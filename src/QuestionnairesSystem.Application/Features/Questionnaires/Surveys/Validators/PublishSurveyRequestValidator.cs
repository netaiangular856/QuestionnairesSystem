using FluentValidation;
using QuestionnairesSystem.Application.Features.Questionnaires.Surveys.DTOs;
using QuestionnairesSystem.Domain.Enums;

namespace QuestionnairesSystem.Application.Features.Questionnaires.Surveys.Validators;

public sealed class PublishSurveyRequestValidator : AbstractValidator<PublishSurveyRequest>
{
    public PublishSurveyRequestValidator()
    {
        RuleFor(x => x.AudienceUserIds)
            .Must(ids => ids is { Count: > 0 })
            .When(x => x.AudienceScope == SurveyAudienceScope.SpecificUsers)
            .WithMessage("AudienceUserIds is required when targeting specific users.");
    }
}
