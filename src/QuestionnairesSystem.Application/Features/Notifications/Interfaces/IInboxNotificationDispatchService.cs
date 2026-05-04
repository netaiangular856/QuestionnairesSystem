using QuestionnairesSystem.Application.Features.Notifications.DTOs;

namespace QuestionnairesSystem.Application.Features.Notifications.Interfaces;

public interface IInboxNotificationDispatchService
{
    /// <summary>
    /// Creates one inbox row per recipient (deduped). Skips unknown or inactive users.
    /// Failures are logged and swallowed so business operations are not blocked.
    /// </summary>
    Task DispatchAsync(
        IEnumerable<Guid> recipientUserIds,
        LocalizedInboxNotificationText text,
        string? relatedEntityType = null,
        Guid? relatedEntityId = null,
        Guid? relatedEntityParentId = null,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Guid>> GetActiveUserIdsWithAnyPermissionAsync(
        IReadOnlyCollection<string> permissionCodes,
        CancellationToken cancellationToken = default);
}
