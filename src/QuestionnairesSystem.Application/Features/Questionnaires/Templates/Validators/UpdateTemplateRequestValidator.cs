using FluentValidation;
using QuestionnairesSystem.Application.Features.Questionnaires.Templates.DTOs;

namespace QuestionnairesSystem.Application.Features.Questionnaires.Templates.Validators;

public sealed class UpdateTemplateRequestValidator : AbstractValidator<UpdateTemplateRequest>
{
    public UpdateTemplateRequestValidator()
    {
        RuleFor(x => x.NameAr).NotEmpty().MaximumLength(300);
        RuleFor(x => x.NameEn).NotEmpty().MaximumLength(300);
        RuleFor(x => x.DescriptionAr).MaximumLength(2000);
        RuleFor(x => x.DescriptionEn).MaximumLength(2000);
        RuleFor(x => x.StructureJson).NotEmpty();
    }
}
