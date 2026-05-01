using FluentValidation.Results;

namespace QuestionnairesSystem.Application.Common;

public static class FluentValidationResultMapper
{
    public static IReadOnlyList<string> ToErrorMessages(this ValidationResult validation) =>
        validation.Errors.Select(static e => e.ErrorMessage).ToList();
}
