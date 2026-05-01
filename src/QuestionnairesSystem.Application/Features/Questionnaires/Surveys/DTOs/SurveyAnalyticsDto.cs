namespace QuestionnairesSystem.Application.Features.Questionnaires.Surveys.DTOs;

public sealed class SurveyAnalyticsDto
{
    public Guid SurveyId { get; init; }
    public int TotalResponses { get; init; }
    public int SubmittedResponses { get; init; }
    public int InProgressResponses { get; init; }
    public int QuestionCount { get; init; }
    public int ParticipantCount { get; init; }
}
