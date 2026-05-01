using System.Diagnostics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QuestionnairesSystem.Api.Extensions;
using QuestionnairesSystem.Application.Features.Identity;
using QuestionnairesSystem.Application.Features.Notifications.DTOs;
using QuestionnairesSystem.Application.Features.Notifications.Interfaces;
using QuestionnairesSystem.Shared.Api;

namespace QuestionnairesSystem.Api.Controllers;

[ApiController]
[Route("api/notifications")]
[Authorize(Policy = PermissionCodes.NotificationView)]
public sealed class NotificationsController : ControllerBase
{
    private readonly INotificationService _notificationService;

    public NotificationsController(INotificationService notificationService)
    {
        _notificationService = notificationService;
    }

    [HttpGet]
    public async Task<IActionResult> GetPaged([FromQuery] NotificationFilterRequest request, CancellationToken cancellationToken)
    {
        var traceId = Activity.Current?.Id ?? HttpContext.TraceIdentifier;
        var result = await _notificationService.GetPagedAsync(request, cancellationToken);
        if (result.IsSuccess)
        {
            return Ok(ApiResponse<PagedResult<NotificationDto>>.FromSuccess(result.Value!, traceId));
        }

        return result.ToApiActionResult(this, traceId);
    }

    [HttpPatch("{notificationId:guid}/read")]
    [Authorize(Policy = PermissionCodes.NotificationManage)]
    public async Task<IActionResult> MarkAsRead(
        Guid notificationId,
        [FromBody] MarkNotificationReadRequest request,
        CancellationToken cancellationToken)
    {
        var traceId = Activity.Current?.Id ?? HttpContext.TraceIdentifier;
        var result = await _notificationService.MarkAsReadAsync(notificationId, request.IsRead, cancellationToken);
        return result.ToApiActionResult(this, traceId);
    }
}
