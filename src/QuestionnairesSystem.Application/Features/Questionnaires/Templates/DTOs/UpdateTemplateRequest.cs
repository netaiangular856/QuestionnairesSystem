namespace QuestionnairesSystem.Application.Features.Questionnaires.Templates.DTOs;

public sealed class UpdateTemplateRequest
{
    public string NameAr { get; set; } = string.Empty;
    public string NameEn { get; set; } = string.Empty;
    public string? DescriptionAr { get; set; }
    public string? DescriptionEn { get; set; }
    public string StructureJson { get; set; } = "{}";
}
