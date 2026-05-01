using QuestionnairesSystem.Domain.Enums;

namespace QuestionnairesSystem.Application.Features.Questionnaires.Surveys.DTOs;

public sealed class PatchSurveyStatusRequest
{
    public SurveyStatus Status { get; set; }
}
