using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using QuestionnairesSystem.Application.Features.Notifications.DTOs;
using QuestionnairesSystem.Application.Features.Notifications.Interfaces;
using QuestionnairesSystem.Domain.Notifications;
using QuestionnairesSystem.Persistence;

namespace QuestionnairesSystem.Application.Features.Notifications.Services;

public sealed class InboxNotificationDispatchService : IInboxNotificationDispatchService
{
    private readonly QuestionnairesDbContext _db;
    private readonly ILogger<InboxNotificationDispatchService> _logger;

    public InboxNotificationDispatchService(
        QuestionnairesDbContext db,
        ILogger<InboxNotificationDispatchService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task DispatchAsync(
        IEnumerable<Guid> recipientUserIds,
        LocalizedInboxNotificationText text,
        string? relatedEntityType = null,
        Guid? relatedEntityId = null,
        Guid? relatedEntityParentId = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var distinct = recipientUserIds
                .Where(x => x != Guid.Empty)
                .Distinct()
                .ToList();
            if (distinct.Count == 0)
                return;

            var valid = await _db.Users.AsNoTracking()
                .Where(u => distinct.Contains(u.Id) && u.IsActive)
                .Select(u => u.Id)
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);
            if (valid.Count == 0)
                return;

            foreach (var userId in valid)
            {
                _db.InboxNotifications.Add(new InboxNotification
                {
                    UserId = userId,
                    TitleAr = text.TitleAr,
                    TitleEn = text.TitleEn,
                    MessageAr = text.MessageAr,
                    MessageEn = text.MessageEn,
                    RelatedEntityType = relatedEntityType,
                    RelatedEntityId = relatedEntityId,
                    RelatedEntityParentId = relatedEntityParentId,
                    IsRead = false
                });
            }

            await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Inbox notification dispatch failed: {TitleEn}", text.TitleEn);
        }
    }

    public async Task<IReadOnlyList<Guid>> GetActiveUserIdsWithAnyPermissionAsync(
        IReadOnlyCollection<string> permissionCodes,
        CancellationToken cancellationToken = default)
    {
        if (permissionCodes.Count == 0)
            return Array.Empty<Guid>();

        return await _db.Users.AsNoTracking()
            .Where(u => u.IsActive)
            .Where(u => u.UserRoles.Any(ur =>
                ur.Role!.RolePermissions.Any(rp => permissionCodes.Contains(rp.Permission.Code))))
            .Select(u => u.Id)
            .Distinct()
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }
}
