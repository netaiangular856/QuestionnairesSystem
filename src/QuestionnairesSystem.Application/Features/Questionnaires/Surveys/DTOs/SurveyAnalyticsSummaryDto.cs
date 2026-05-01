namespace QuestionnairesSystem.Application.Features.Questionnaires.Surveys.DTOs;

public sealed class SurveyAnalyticsSummaryDto
{
    public Guid SurveyId { get; init; }
    public double CompletionRate { get; init; }
    public int SubmittedCount { get; init; }
    public int InvitedParticipants { get; init; }
}
