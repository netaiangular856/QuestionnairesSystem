using QuestionnairesSystem.Domain.Enums;

namespace QuestionnairesSystem.Application.Features.Questionnaires.Surveys.DTOs;

public sealed class SurveyListItemDto
{
    public Guid Id { get; init; }
    public string TitleAr { get; init; } = string.Empty;
    public string TitleEn { get; init; } = string.Empty;
    public string? Code { get; init; }
    public SurveyStatus Status { get; init; }
    public int Version { get; init; }

    /// <summary>Survey owner — for display; prefer this over <c>OwnerUserId</c> in UI.</summary>
    public string? OwnerDisplayName { get; init; }

    public DateTime? PublishedAtUtc { get; init; }
    public int QuestionCount { get; init; }
    public int ResponseCount { get; init; }
}
