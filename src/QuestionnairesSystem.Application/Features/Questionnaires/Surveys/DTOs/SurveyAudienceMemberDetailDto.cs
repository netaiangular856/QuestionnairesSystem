namespace QuestionnairesSystem.Application.Features.Questionnaires.Surveys.DTOs;

public sealed class SurveyAudienceMemberDetailDto
{
    public Guid? UserId { get; init; }

    public string? Email { get; init; }

    public string DisplayName { get; init; } = string.Empty;
}
