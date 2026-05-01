using Microsoft.AspNetCore.Authorization;
using QuestionnairesSystem.Shared.Identity;

namespace QuestionnairesSystem.Infrastructure.Authorization;

public sealed class PermissionAuthorizationHandler : AuthorizationHandler<PermissionRequirement>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        PermissionRequirement requirement)
    {
        var ok = context.User.Claims.Any(c =>
            c.Type == QuestionnairesClaimTypes.Permission &&
            string.Equals(c.Value, requirement.PermissionCode, StringComparison.Ordinal));

        if (ok)
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}

