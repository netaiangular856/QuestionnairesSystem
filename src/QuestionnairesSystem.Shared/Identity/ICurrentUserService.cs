namespace QuestionnairesSystem.Shared.Identity;

public interface ICurrentUserService
{
    Guid? UserId { get; }

    string? UserName { get; }

    bool IsAuthenticated { get; }
}

