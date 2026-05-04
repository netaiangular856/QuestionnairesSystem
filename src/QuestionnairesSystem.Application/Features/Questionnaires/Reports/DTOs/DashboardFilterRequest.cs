namespace QuestionnairesSystem.Application.Features.Questionnaires.Reports.DTOs;

/// <summary>Optional UTC range for dashboard aggregates. <see cref="ToUtc"/> is an exclusive end instant (same convention as cross-survey analytics).</summary>
public sealed class DashboardFilterRequest
{
    public DateTime? FromUtc { get; init; }
    public DateTime? ToUtc { get; init; }
}
