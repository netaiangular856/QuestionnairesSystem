using QuestionnairesSystem.Application.Features.Notifications.DTOs;
using QuestionnairesSystem.Shared.Api;
using QuestionnairesSystem.Shared.Results;

namespace QuestionnairesSystem.Application.Features.Notifications.Interfaces;

public interface INotificationService
{
    Task<Result<PagedResult<NotificationDto>>> GetPagedAsync(NotificationFilterRequest request, CancellationToken cancellationToken = default);
    Task<Result> MarkAsReadAsync(Guid notificationId, bool isRead, CancellationToken cancellationToken = default);
}
