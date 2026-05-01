using FluentValidation;
using QuestionnairesSystem.Application.Features.Questionnaires.Participation.DTOs;

namespace QuestionnairesSystem.Application.Features.Questionnaires.Participation.Validators;

public sealed class CreateParticipantRequestValidator : AbstractValidator<CreateParticipantRequest>
{
    public CreateParticipantRequestValidator()
    {
        RuleFor(x => x.Email).MaximumLength(320).EmailAddress().When(x => !string.IsNullOrWhiteSpace(x.Email));
        RuleFor(x => x.ExternalReference).MaximumLength(256);
        RuleFor(x => x)
            .Must(x => x.UserId.HasValue || !string.IsNullOrWhiteSpace(x.Email) || !string.IsNullOrWhiteSpace(x.ExternalReference))
            .WithMessage("Specify at least one of userId, email, or externalReference.");
    }
}
