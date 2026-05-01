using FluentValidation;
using QuestionnairesSystem.Application.Features.Questionnaires.Questions.DTOs;

namespace QuestionnairesSystem.Application.Features.Questionnaires.Questions.Validators;

public sealed class CreateQuestionRequestValidator : AbstractValidator<CreateQuestionRequest>
{
    public CreateQuestionRequestValidator()
    {
        RuleFor(x => x.Type).IsInEnum();
        RuleFor(x => x.TitleAr).NotEmpty().MaximumLength(1000);
        RuleFor(x => x.TitleEn).NotEmpty().MaximumLength(1000);
        RuleFor(x => x.HelpTextAr).MaximumLength(2000);
        RuleFor(x => x.HelpTextEn).MaximumLength(2000);
        RuleFor(x => x.OptionsJson).MaximumLength(100_000);
        RuleFor(x => x.DisplayOrder).GreaterThan(0).When(x => x.DisplayOrder.HasValue);
    }
}
