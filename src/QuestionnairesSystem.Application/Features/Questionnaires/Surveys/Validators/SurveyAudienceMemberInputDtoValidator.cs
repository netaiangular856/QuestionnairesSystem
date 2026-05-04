using FluentValidation;
using QuestionnairesSystem.Application.Features.Questionnaires.Surveys.DTOs;

namespace QuestionnairesSystem.Application.Features.Questionnaires.Surveys.Validators;

public sealed class SurveyAudienceMemberInputDtoValidator : AbstractValidator<SurveyAudienceMemberInputDto>
{
    public SurveyAudienceMemberInputDtoValidator()
    {
        RuleFor(x => x)
            .Must(m =>
                m.UserId.HasValue && string.IsNullOrWhiteSpace(m.Email) ||
                !m.UserId.HasValue && !string.IsNullOrWhiteSpace(m.Email))
            .WithMessage("Each audience entry must set either UserId (and no Email) or Email (and no UserId).");

        When(x => x.UserId.HasValue, () => RuleFor(x => x.Email).Must(e => string.IsNullOrWhiteSpace(e)));

        When(x => !string.IsNullOrWhiteSpace(x.Email), () =>
        {
            RuleFor(x => x.Email!).EmailAddress().MaximumLength(320);
        });
    }
}
