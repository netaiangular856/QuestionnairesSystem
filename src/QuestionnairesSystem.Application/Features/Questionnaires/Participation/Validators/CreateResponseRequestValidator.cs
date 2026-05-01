using FluentValidation;
using QuestionnairesSystem.Application.Features.Questionnaires.Participation.DTOs;

namespace QuestionnairesSystem.Application.Features.Questionnaires.Participation.Validators;

public sealed class CreateResponseRequestValidator : AbstractValidator<CreateResponseRequest>
{
    public CreateResponseRequestValidator()
    {
        When(x => x.Answers is { Count: > 0 }, () =>
        {
            RuleForEach(x => x.Answers!).SetValidator(new AnswerUpsertDtoValidator());
            RuleFor(x => x.Answers!.Count).LessThanOrEqualTo(2000);
        });
    }
}
