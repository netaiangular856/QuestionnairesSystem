namespace QuestionnairesSystem.Application.Features.Questionnaires.Templates.DTOs;

public sealed class TemplateDetailDto
{
    public Guid Id { get; init; }
    public string NameAr { get; init; } = string.Empty;
    public string NameEn { get; init; } = string.Empty;
    public string? DescriptionAr { get; init; }
    public string? DescriptionEn { get; init; }
    public string StructureJson { get; init; } = "{}";
    public bool IsArchived { get; init; }
    public int UsageCount { get; init; }
}
