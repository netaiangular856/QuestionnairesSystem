using FluentValidation;
using QuestionnairesSystem.Application.Features.Identity.DTOs;
using QuestionnairesSystem.Shared.Constants;

namespace QuestionnairesSystem.Application.Features.Identity.Validators;

public sealed class RoleFilterRequestValidator : AbstractValidator<RoleFilterRequest>
{
    public RoleFilterRequestValidator()
    {
        RuleFor(x => x.Page).GreaterThan(0);
        RuleFor(x => x.PageSize).GreaterThan(0).LessThanOrEqualTo(PaginationConstants.MaxPageSize);
    }
}

