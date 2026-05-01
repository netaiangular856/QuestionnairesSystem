namespace QuestionnairesSystem.Application.Features.Identity.Interfaces;

public sealed record AccessTokenResult(string Token, DateTime ExpiresAtUtc);

public interface IJwtTokenIssuer
{
    AccessTokenResult IssueAccessToken(Guid userId, string userName, string email, IReadOnlyList<string> permissionCodes);
}

