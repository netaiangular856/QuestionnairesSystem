namespace QuestionnairesSystem.Application.Features.Questionnaires.PublicPortal.DTOs;

public sealed class PublicSurveyPageDto
{
    public Guid Id { get; init; }
    public string TitleAr { get; init; } = string.Empty;
    public string TitleEn { get; init; } = string.Empty;
    public string? DescriptionAr { get; init; }
    public string? DescriptionEn { get; init; }
    public string Code { get; init; } = string.Empty;
    public DateTime? OpensAtUtc { get; init; }
    public DateTime? ClosesAtUtc { get; init; }
    public bool PublicArticleEnabled { get; init; }
    public string? PublicArticleTitleAr { get; init; }
    public string? PublicArticleTitleEn { get; init; }
    public string? PublicArticleBodyAr { get; init; }
    public string? PublicArticleBodyEn { get; init; }
}
