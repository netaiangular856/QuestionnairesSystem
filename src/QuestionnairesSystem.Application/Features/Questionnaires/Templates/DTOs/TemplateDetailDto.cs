using QuestionnairesSystem.Application.Features.Questionnaires.Surveys.DTOs;

namespace QuestionnairesSystem.Application.Features.Questionnaires.Templates.DTOs;

public sealed class TemplateDetailDto
{
    public Guid Id { get; init; }
    public string NameAr { get; init; } = string.Empty;
    public string NameEn { get; init; } = string.Empty;
    public string? DescriptionAr { get; init; }
    public string? DescriptionEn { get; init; }
    public IReadOnlyList<CreateSurveyQuestionItem> Questions { get; init; } = Array.Empty<CreateSurveyQuestionItem>();
    public bool IsArchived { get; init; }
    public int UsageCount { get; init; }
    public int QuestionCount { get; init; }
}
