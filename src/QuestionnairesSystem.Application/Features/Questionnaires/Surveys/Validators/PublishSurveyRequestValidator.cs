using FluentValidation;
using QuestionnairesSystem.Application.Features.Questionnaires.Surveys.DTOs;
using QuestionnairesSystem.Domain.Enums;

namespace QuestionnairesSystem.Application.Features.Questionnaires.Surveys.Validators;

public sealed class PublishSurveyRequestValidator : AbstractValidator<PublishSurveyRequest>
{
    public PublishSurveyRequestValidator(SurveyAudienceMemberInputDtoValidator memberValidator)
    {
        When(x => x.AudienceMembers is { Count: > 0 }, () =>
        {
            RuleForEach(x => x.AudienceMembers!).SetValidator(memberValidator);
            RuleFor(x => x.AudienceScope)
                .Must(s => !s.HasValue || s == SurveyAudienceScope.SpecificUsers)
                .WithMessage("AudienceMembers require SpecificUsers audience scope.");
        });

        When(x => x.AudienceUserIds is { Count: > 0 }, () =>
        {
            RuleFor(x => x.AudienceScope)
                .Must(s => !s.HasValue || s == SurveyAudienceScope.SpecificUsers)
                .WithMessage("AudienceUserIds require SpecificUsers audience scope.");
        });
    }
}
