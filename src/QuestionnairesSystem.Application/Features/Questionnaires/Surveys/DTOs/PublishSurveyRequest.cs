using QuestionnairesSystem.Domain.Enums;

namespace QuestionnairesSystem.Application.Features.Questionnaires.Surveys.DTOs;

/// <summary>جسم نشر اختياري: تحديد الجمهور عند النشر.</summary>
public sealed class PublishSurveyRequest
{
    public SurveyAudienceScope? AudienceScope { get; set; }

    /// <summary>عند <see cref="SurveyAudienceScope.SpecificUsers"/> يُحدد المستخدمون المسموح لهم.</summary>
    public IReadOnlyList<Guid>? AudienceUserIds { get; set; }
}
