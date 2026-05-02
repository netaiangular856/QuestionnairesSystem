using QuestionnairesSystem.Application.Features.Questionnaires.Surveys.DTOs;

namespace QuestionnairesSystem.Application.Features.Questionnaires.Templates.DTOs;

public sealed class CreateTemplateRequest
{
    public string NameAr { get; set; } = string.Empty;
    public string NameEn { get; set; } = string.Empty;
    public string? DescriptionAr { get; set; }
    public string? DescriptionEn { get; set; }

    /// <summary>أسئلة القالب؛ يُخزَّن التمثيل الداخلي في قاعدة البيانات كـ JSON دون إظهاره للمستخدم.</summary>
    public IReadOnlyList<CreateSurveyQuestionItem> Questions { get; set; } = Array.Empty<CreateSurveyQuestionItem>();
}
