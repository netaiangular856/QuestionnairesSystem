using Microsoft.EntityFrameworkCore;
using QuestionnairesSystem.Application.Features.Notifications.DTOs;
using QuestionnairesSystem.Application.Features.Notifications.Interfaces;
using QuestionnairesSystem.Persistence;
using QuestionnairesSystem.Shared.Api;
using QuestionnairesSystem.Shared.Constants;
using QuestionnairesSystem.Shared.Identity;
using QuestionnairesSystem.Shared.Results;

namespace QuestionnairesSystem.Application.Features.Notifications.Services;

public sealed class NotificationService : INotificationService
{
    private readonly QuestionnairesDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public NotificationService(QuestionnairesDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<Result<PagedResult<NotificationDto>>> GetPagedAsync(
        NotificationFilterRequest request,
        CancellationToken cancellationToken = default)
    {
        var page = request.Page <= 0 ? PaginationConstants.DefaultPage : request.Page;
        var pageSize = request.PageSize <= 0 ? PaginationConstants.DefaultPageSize : request.PageSize;
        if (pageSize > PaginationConstants.MaxPageSize)
        {
            pageSize = PaginationConstants.MaxPageSize;
        }

        var query = _db.InboxNotifications.AsNoTracking().AsQueryable();

        if (_currentUser.UserId.HasValue)
        {
            query = query.Where(x => x.UserId == _currentUser.UserId.Value);
        }

        if (request.IsRead.HasValue)
        {
            query = query.Where(x => x.IsRead == request.IsRead.Value);
        }

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var term = request.Search.Trim();
            query = query.Where(x =>
                x.TitleAr.Contains(term) ||
                x.TitleEn.Contains(term) ||
                x.MessageAr.Contains(term) ||
                x.MessageEn.Contains(term));
        }

        var total = await query.CountAsync(cancellationToken).ConfigureAwait(false);
        var items = await query
            .OrderByDescending(x => x.CreatedOnUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new NotificationDto
            {
                Id = x.Id,
                UserId = x.UserId,
                TitleAr = x.TitleAr,
                TitleEn = x.TitleEn,
                MessageAr = x.MessageAr,
                MessageEn = x.MessageEn,
                IsRead = x.IsRead,
                ReadAtUtc = x.ReadAtUtc,
                RelatedEntityType = x.RelatedEntityType,
                RelatedEntityId = x.RelatedEntityId,
                RelatedEntityParentId = x.RelatedEntityParentId,
                CreatedOnUtc = x.CreatedOnUtc
            })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return Result<PagedResult<NotificationDto>>.Ok(new PagedResult<NotificationDto>
        {
            Items = items,
            TotalCount = total,
            Page = page,
            PageSize = pageSize
        });
    }

    public async Task<Result> MarkAsReadAsync(Guid notificationId, bool isRead, CancellationToken cancellationToken = default)
    {
        var notification = await _db.InboxNotifications
            .FirstOrDefaultAsync(x => x.Id == notificationId, cancellationToken)
            .ConfigureAwait(false);

        if (notification is null)
        {
            return Result.Fail("Notification was not found.");
        }

        if (_currentUser.UserId.HasValue && notification.UserId != _currentUser.UserId.Value)
        {
            return Result.Fail("You are not allowed to update this notification.");
        }

        notification.IsRead = isRead;
        notification.ReadAtUtc = isRead ? DateTime.UtcNow : null;

        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Result.Ok();
    }
}
