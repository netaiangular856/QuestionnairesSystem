using FluentValidation;
using QuestionnairesSystem.Application.Features.Questionnaires.Recommendations.DTOs;

namespace QuestionnairesSystem.Application.Features.Questionnaires.Recommendations.Validators;

public sealed class UpdateRecommendationRequestValidator : AbstractValidator<UpdateRecommendationRequest>
{
    public UpdateRecommendationRequestValidator()
    {
        RuleFor(x => x.TitleAr).NotEmpty().MaximumLength(500);
        RuleFor(x => x.TitleEn).NotEmpty().MaximumLength(500);
        RuleFor(x => x.DescriptionAr).MaximumLength(4000);
        RuleFor(x => x.DescriptionEn).MaximumLength(4000);
        RuleFor(x => x.Status).IsInEnum();
    }
}
