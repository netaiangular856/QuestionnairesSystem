using QuestionnairesSystem.Domain.Enums;

namespace QuestionnairesSystem.Application.Features.Questionnaires.Surveys.DTOs;

/// <summary>صف نتيجة بحث موحّد للجمهور (مستخدمون، موظفون، متعاملون).</summary>
public sealed class SurveyAudienceLookupItemDto
{
    public SurveyAudienceSubjectKind Kind { get; init; }

    /// <summary>معرف الكيان في جدوله (User / Employee / Partner).</summary>
    public Guid EntityId { get; init; }

    public string Name { get; init; } = string.Empty;

    public string? Email { get; init; }

    /// <summary>عند الوجود يُخزَّن كـ UserId في جدول الجمهور؛ وإلا يُستخدم البريد.</summary>
    public Guid? UserId { get; init; }
}
