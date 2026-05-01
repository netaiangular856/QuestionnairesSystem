using QuestionnairesSystem.Domain.Enums;
using QuestionnairesSystem.Shared.Constants;

namespace QuestionnairesSystem.Application.Features.Questionnaires.Surveys.DTOs;

public sealed class SurveyFilterRequest
{
    public int Page { get; set; } = PaginationConstants.DefaultPage;
    public int PageSize { get; set; } = PaginationConstants.DefaultPageSize;
    public SurveyStatus? Status { get; set; }
    public string? Search { get; set; }
}
