using FluentValidation;
using QuestionnairesSystem.Application.Features.Questionnaires.Surveys.DTOs;

namespace QuestionnairesSystem.Application.Features.Questionnaires.Surveys.Validators;

public sealed class CreateSurveyRequestValidator : AbstractValidator<CreateSurveyRequest>
{
    public CreateSurveyRequestValidator(CreateSurveyQuestionItemValidator questionItemValidator)
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
    }
}
