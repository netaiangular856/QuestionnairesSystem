using QuestionnairesSystem.Domain.Enums;

namespace QuestionnairesSystem.Application.Features.Questionnaires.Surveys.DTOs;

public sealed class SurveyDetailDto
{
    public Guid Id { get; init; }
    public string TitleAr { get; init; } = string.Empty;
    public string TitleEn { get; init; } = string.Empty;
    public string? DescriptionAr { get; init; }
    public string? DescriptionEn { get; init; }
    public string? Code { get; init; }
    public SurveyStatus Status { get; init; }
    public SurveyAudienceScope AudienceScope { get; init; }
    public int Version { get; init; }
    public Guid? OwnerUserId { get; init; }

    /// <summary>Friendly label for <see cref="OwnerUserId"/> (name / username).</summary>
    public string? OwnerDisplayName { get; init; }

    public Guid? TemplateId { get; init; }
    public DateTime? PublishedAtUtc { get; init; }
    public DateTime? ClosedAtUtc { get; init; }
    public string? RejectionReason { get; init; }
}
