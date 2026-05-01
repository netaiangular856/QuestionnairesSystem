namespace QuestionnairesSystem.Application.Features.AuditLogs.DTOs;

public sealed class AuditLogDto
{
    public Guid Id { get; init; }
    public DateTime OccurredAtUtc { get; init; }
    public Guid? UserId { get; init; }
    public string? UserName { get; init; }
    public string Action { get; init; } = string.Empty;
    public string EntityType { get; init; } = string.Empty;
    public string EntityId { get; init; } = string.Empty;
    public string? OldValuesJson { get; init; }
    public string? NewValuesJson { get; init; }
    public string? IpAddress { get; init; }
    public string? UserAgent { get; init; }
}

public sealed class AuditLogFilterRequest
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public Guid? UserId { get; set; }
    public string? Action { get; set; }
    public string? EntityType { get; set; }
    public DateTime? FromUtc { get; set; }
    public DateTime? ToUtc { get; set; }
}
