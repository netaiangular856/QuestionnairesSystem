namespace QuestionnairesSystem.Application.Features.Questionnaires.Surveys.DTOs;

/// <summary>Word stem/token frequency from open-text survey answers (frequency analysis, not AI).</summary>
public sealed class KeywordCountDto
{
    public string Keyword { get; init; } = string.Empty;
    public int Count { get; init; }
}
