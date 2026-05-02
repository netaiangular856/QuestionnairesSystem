using QuestionnairesSystem.Domain.Enums;

namespace QuestionnairesSystem.Application.Features.Questionnaires.Surveys.DTOs;

public sealed class CreateSurveyRequest
{
    public string TitleAr { get; set; } = string.Empty;
    public string TitleEn { get; set; } = string.Empty;
    public string? DescriptionAr { get; set; }
    public string? DescriptionEn { get; set; }
    public string? Code { get; set; }
    public SurveyAudienceScope AudienceScope { get; set; } = SurveyAudienceScope.AllOrganizationMembers;
    public Guid? TemplateId { get; set; }

    /// <summary>أسئلة المنشئ بعد أسئلة القالب (إن وُجد).</summary>
    public IReadOnlyList<CreateSurveyQuestionItem>? Questions { get; set; }

    /// <summary>بداية المخطط الزمني (UTC).</summary>
    public DateTime? OpensAtUtc { get; set; }

    /// <summary>نهاية المخطط؛ بعدها يُؤرشف الاستبيان تلقائياً.</summary>
    public DateTime? ClosesAtUtc { get; set; }
}
