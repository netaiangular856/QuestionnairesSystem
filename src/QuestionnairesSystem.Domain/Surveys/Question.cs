using QuestionnairesSystem.Domain.Common;
using QuestionnairesSystem.Domain.Enums;

namespace QuestionnairesSystem.Domain.Surveys;

public sealed class Question : AuditableDomainEntity
{
    public Guid SurveyId { get; set; }
    public int DisplayOrder { get; set; }
    public QuestionType Type { get; set; }
    public string TitleAr { get; set; } = null!;
    public string TitleEn { get; set; } = null!;
    public string? HelpTextAr { get; set; }
    public string? HelpTextEn { get; set; }
    public bool IsRequired { get; set; }
    /// <summary>
    /// JSON حسب النوع: للـ Scale نطاق مثل {"min":1,"max":10}؛ للـ Choice قائمة خيارات؛ للـ Text قواعد اختيارية (طول، سطر متعدد).
    /// </summary>
    public string? OptionsJson { get; set; }

    public Survey Survey { get; set; } = null!;
    public ICollection<Responses.QuestionAnswer> Answers { get; set; } = new List<Responses.QuestionAnswer>();
}
