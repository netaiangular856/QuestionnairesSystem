using Microsoft.AspNetCore.Mvc;
using QuestionnairesSystem.Application.Common;
using QuestionnairesSystem.Shared.Api;
using QuestionnairesSystem.Shared.Results;

namespace QuestionnairesSystem.Api.Extensions;

public static class ResultHttpExtensions
{
    private static readonly HashSet<string?> NotFoundCodes = new(StringComparer.Ordinal)
    {
        IdentityErrors.UserNotFound,
        IdentityErrors.RoleNotFound,
        IdentityErrors.PermissionNotFound,
        QuestionnaireErrors.SurveyNotFound,
        QuestionnaireErrors.QuestionNotFound,
        QuestionnaireErrors.TemplateNotFound,
        QuestionnaireErrors.ParticipantNotFound,
        QuestionnaireErrors.ResponseNotFound,
        QuestionnaireErrors.RecommendationNotFound,
        QuestionnaireErrors.ActionPlanNotFound,
        QuestionnaireErrors.InitiativeNotFound
    };

    private static readonly HashSet<string?> ConflictCodes = new(StringComparer.Ordinal)
    {
        IdentityErrors.DuplicateUserName,
        IdentityErrors.DuplicateEmail,
        IdentityErrors.DuplicateRoleName,
        QuestionnaireErrors.SurveyCodeAlreadyExists
    };

    private static readonly HashSet<string?> UnauthorizedCodes = new(StringComparer.Ordinal)
    {
        IdentityErrors.InvalidCredentials,
        IdentityErrors.UserInactive
    };

    public static IActionResult ToApiActionResult<T>(this Result<T> result, ControllerBase controller, string? traceId)
    {
        if (result.IsSuccess)
        {
            return controller.Ok(ApiResponse<T>.FromSuccess(result.Value!, traceId));
        }

        var payload = ApiResponse<T>.FromFailure(result.Errors, traceId);

        if (NotFoundCodes.Contains(result.FailureCode)) return controller.NotFound(payload);
        if (ConflictCodes.Contains(result.FailureCode)) return controller.Conflict(payload);
        if (UnauthorizedCodes.Contains(result.FailureCode)) return controller.Unauthorized(payload);

        return controller.BadRequest(payload);
    }

    public static IActionResult ToApiActionResult(this Result result, ControllerBase controller, string? traceId)
    {
        if (result.IsSuccess)
        {
            return controller.Ok(ApiResponse.FromSuccess(traceId));
        }

        var payload = ApiResponse.FromFailure(result.Errors, traceId);

        if (NotFoundCodes.Contains(result.FailureCode)) return controller.NotFound(payload);
        if (ConflictCodes.Contains(result.FailureCode)) return controller.Conflict(payload);
        if (UnauthorizedCodes.Contains(result.FailureCode)) return controller.Unauthorized(payload);

        return controller.BadRequest(payload);
    }
}
