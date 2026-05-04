namespace QuestionnairesSystem.Application.Features.Questionnaires.PublicPortal.DTOs;

public sealed class PublicSurveyListItemDto
{
    public Guid Id { get; init; }
    public string TitleAr { get; init; } = string.Empty;
    public string TitleEn { get; init; } = string.Empty;
    public string Code { get; init; } = string.Empty;
    public string? DescriptionAr { get; init; }
    public string? DescriptionEn { get; init; }
    public DateTime? ClosesAtUtc { get; init; }
}
