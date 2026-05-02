using QuestionnairesSystem.Domain.Enums;

namespace QuestionnairesSystem.Application.Features.Questionnaires.Surveys.DTOs;

/// <summary>أسئلة إضافية يحددها المنشئ بعد دمج قالب (اختياري) في نفس طلب إنشاء الاستبيان.</summary>
public sealed class CreateSurveyQuestionItem
{
    public QuestionType Type { get; set; }
    public string TitleAr { get; set; } = string.Empty;
    public string TitleEn { get; set; } = string.Empty;
    public string? HelpTextAr { get; set; }
    public string? HelpTextEn { get; set; }
    public bool IsRequired { get; set; }
    public string? OptionsJson { get; set; }
    public int? DisplayOrder { get; set; }
}
