using FluentValidation;
using QuestionnairesSystem.Application.Features.Questionnaires.Questions.DTOs;

namespace QuestionnairesSystem.Application.Features.Questionnaires.Questions.Validators;

public sealed class ReorderQuestionsRequestValidator : AbstractValidator<ReorderQuestionsRequest>
{
    public ReorderQuestionsRequestValidator()
    {
        RuleFor(x => x.QuestionIds).NotNull().NotEmpty();
        RuleFor(x => x.QuestionIds!.Count).LessThanOrEqualTo(500);
        RuleForEach(x => x.QuestionIds!).NotEmpty();
    }
}
