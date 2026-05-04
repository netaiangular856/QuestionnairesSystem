using FluentValidation;
using QuestionnairesSystem.Application.Features.Questionnaires.Surveys.DTOs;
using QuestionnairesSystem.Domain.Enums;

namespace QuestionnairesSystem.Application.Features.Questionnaires.Surveys.Validators;

public sealed class UpdateSurveyRequestValidator : AbstractValidator<UpdateSurveyRequest>
{
    public UpdateSurveyRequestValidator(SurveyAudienceMemberInputDtoValidator audienceMemberValidator)
    {
        RuleFor(x => x.TitleAr).NotEmpty().MaximumLength(500);
        RuleFor(x => x.TitleEn).NotEmpty().MaximumLength(500);
        RuleFor(x => x.DescriptionAr).MaximumLength(4000);
        RuleFor(x => x.DescriptionEn).MaximumLength(4000);
        RuleFor(x => x.Code).MaximumLength(64);
        RuleFor(x => x.PublicArticleTitleAr).MaximumLength(500);
        RuleFor(x => x.PublicArticleTitleEn).MaximumLength(500);
        RuleFor(x => x.PublicArticleBodyAr).MaximumLength(100_000);
        RuleFor(x => x.PublicArticleBodyEn).MaximumLength(100_000);

        When(x => x.AudienceMembers is { Count: > 0 }, () =>
        {
            RuleForEach(x => x.AudienceMembers!).SetValidator(audienceMemberValidator);
        });

        When(x => x.AudienceScope == SurveyAudienceScope.SpecificUsers && x.AudienceMembers is not null, () =>
        {
            RuleFor(x => x.AudienceMembers!)
                .Must(m => m.Count > 0)
                .WithMessage("When audience is SpecificUsers, AudienceMembers cannot be empty.");
        });
    }
}
