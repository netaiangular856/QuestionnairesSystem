using FluentValidation;
using QuestionnairesSystem.Application.Features.Questionnaires.Surveys.DTOs;
using QuestionnairesSystem.Domain.Enums;

namespace QuestionnairesSystem.Application.Features.Questionnaires.Surveys.Validators;

public sealed class CreateSurveyRequestValidator : AbstractValidator<CreateSurveyRequest>
{
    public CreateSurveyRequestValidator(
        CreateSurveyQuestionItemValidator questionItemValidator,
        SurveyAudienceMemberInputDtoValidator audienceMemberValidator)
    {
        RuleFor(x => x.TitleAr).NotEmpty().MaximumLength(500);
        RuleFor(x => x.TitleEn).NotEmpty().MaximumLength(500);
        RuleFor(x => x.DescriptionAr).MaximumLength(4000);
        RuleFor(x => x.DescriptionEn).MaximumLength(4000);
        RuleFor(x => x.Code).MaximumLength(64);
        When(x => x.Questions is { Count: > 0 }, () =>
        {
            RuleForEach(x => x.Questions!).SetValidator(questionItemValidator);
        });
        RuleFor(x => x)
            .Must(x => !x.OpensAtUtc.HasValue || !x.ClosesAtUtc.HasValue || x.ClosesAtUtc > x.OpensAtUtc)
            .WithMessage("ClosesAtUtc must be after OpensAtUtc.");

        When(x => x.AudienceScope == SurveyAudienceScope.SpecificUsers, () =>
        {
            RuleFor(x => x.AudienceMembers)
                .NotNull()
                .Must(m => m!.Count > 0)
                .WithMessage("AudienceMembers is required when audience is SpecificUsers.");
            RuleForEach(x => x.AudienceMembers!).SetValidator(audienceMemberValidator);
        });

        When(x => x.AudienceScope != SurveyAudienceScope.SpecificUsers, () =>
        {
            RuleFor(x => x.AudienceMembers)
                .Must(m => m == null || m.Count == 0)
                .WithMessage("AudienceMembers should only be sent when audience is SpecificUsers.");
        });
    }
}
