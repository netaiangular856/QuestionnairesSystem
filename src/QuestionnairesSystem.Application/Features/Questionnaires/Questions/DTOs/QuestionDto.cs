using QuestionnairesSystem.Domain.Enums;

namespace QuestionnairesSystem.Application.Features.Questionnaires.Questions.DTOs;

public sealed class QuestionDto
{
    public Guid Id { get; init; }
    public Guid SurveyId { get; init; }
    public int DisplayOrder { get; init; }
    public QuestionType Type { get; init; }
    public string TitleAr { get; init; } = string.Empty;
    public string TitleEn { get; init; } = string.Empty;
    public string? HelpTextAr { get; init; }
    public string? HelpTextEn { get; init; }
    public bool IsRequired { get; init; }
    public string? OptionsJson { get; init; }
}
