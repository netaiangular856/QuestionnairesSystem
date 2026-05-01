using FluentValidation;
using QuestionnairesSystem.Application.Features.Questionnaires.Templates.DTOs;

namespace QuestionnairesSystem.Application.Features.Questionnaires.Templates.Validators;

public sealed class CreateTemplateRequestValidator : AbstractValidator<CreateTemplateRequest>
{
    public CreateTemplateRequestValidator()
    {
        RuleFor(x => x.NameAr).NotEmpty().MaximumLength(300);
        RuleFor(x => x.NameEn).NotEmpty().MaximumLength(300);
        RuleFor(x => x.DescriptionAr).MaximumLength(2000);
        RuleFor(x => x.DescriptionEn).MaximumLength(2000);
        RuleFor(x => x.StructureJson).NotEmpty();
    }
}
