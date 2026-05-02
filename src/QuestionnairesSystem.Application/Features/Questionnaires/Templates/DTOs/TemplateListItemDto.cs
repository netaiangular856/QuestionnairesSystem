namespace QuestionnairesSystem.Application.Features.Questionnaires.Templates.DTOs;

public sealed class TemplateListItemDto
{
    public Guid Id { get; init; }
    public string NameAr { get; init; } = string.Empty;
    public string NameEn { get; init; } = string.Empty;
    public bool IsArchived { get; init; }
    public int UsageCount { get; init; }
    public int QuestionCount { get; init; }
}
