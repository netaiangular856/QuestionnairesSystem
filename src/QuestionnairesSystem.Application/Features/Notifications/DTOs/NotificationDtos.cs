namespace QuestionnairesSystem.Application.Features.Notifications.DTOs;

public sealed class NotificationDto
{
    public Guid Id { get; init; }
    public Guid UserId { get; init; }
    public string Title { get; init; } = string.Empty;
    public string Message { get; init; } = string.Empty;
    public bool IsRead { get; init; }
    public DateTime? ReadAtUtc { get; init; }
    public string? RelatedEntityType { get; init; }
    public Guid? RelatedEntityId { get; init; }
    public DateTime CreatedOnUtc { get; init; }
}

public sealed class NotificationFilterRequest
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public bool? IsRead { get; set; }
    public string? Search { get; set; }
}

public sealed class MarkNotificationReadRequest
{
    public bool IsRead { get; set; } = true;
}
