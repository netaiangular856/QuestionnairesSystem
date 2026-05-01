using FluentValidation;
using QuestionnairesSystem.Application.Features.Questionnaires.Participation.DTOs;

namespace QuestionnairesSystem.Application.Features.Questionnaires.Participation.Validators;

public sealed class AnswerUpsertDtoValidator : AbstractValidator<AnswerUpsertDto>
{
    public AnswerUpsertDtoValidator()
    {
        RuleFor(x => x.QuestionId).NotEmpty();
        RuleFor(x => x.ValueJson).NotEmpty().MaximumLength(100_000);
    }
}
