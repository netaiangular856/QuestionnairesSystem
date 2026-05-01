using QuestionnairesSystem.Domain.Enums;

namespace QuestionnairesSystem.Application.Features.Questionnaires.Questions.DTOs;

public sealed class CreateQuestionRequest
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
