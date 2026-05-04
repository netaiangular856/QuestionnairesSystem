using QuestionnairesSystem.Domain.Enums;

namespace QuestionnairesSystem.Application.Features.Questionnaires.Surveys.DTOs;

public sealed class UpdateSurveyRequest
{
    public string TitleAr { get; set; } = string.Empty;
    public string TitleEn { get; set; } = string.Empty;
    public string? DescriptionAr { get; set; }
    public string? DescriptionEn { get; set; }
    public string? Code { get; set; }
    public SurveyAudienceScope AudienceScope { get; set; }
    public IReadOnlyList<CreateSurveyQuestionItem>? Questions { get; set; }

    /// <summary>عند الإرسال يُستبدل جدول الجمهور بالكامل؛ عند <see langword="null"/> لا يُغيّر الجمهور.</summary>
    public IReadOnlyList<SurveyAudienceMemberInputDto>? AudienceMembers { get; set; }

    public bool ShowOnPublicPortal { get; set; }
    public bool PublicArticleEnabled { get; set; }
    public string? PublicArticleTitleAr { get; set; }
    public string? PublicArticleTitleEn { get; set; }
    public string? PublicArticleBodyAr { get; set; }
    public string? PublicArticleBodyEn { get; set; }
}
