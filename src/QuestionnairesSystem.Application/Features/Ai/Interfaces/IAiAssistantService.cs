using QuestionnairesSystem.Application.Features.Ai.DTOs;
using QuestionnairesSystem.Shared.Results;

namespace QuestionnairesSystem.Application.Features.Ai.Interfaces;

public interface IAiAssistantService
{
    Task<Result<TranslateRichTextResponse>> TranslateRichTextAsync(
        TranslateRichTextRequest request,
        CancellationToken cancellationToken = default);

    Task<Result<AiSuggestRecommendationDraftDto>> SuggestRecommendationAsync(
        AiSuggestFromSurveyRequest request,
        CancellationToken cancellationToken = default);

    Task<Result<AiSuggestActionPlanDraftDto>> SuggestActionPlanAsync(
        AiSuggestFromSurveyRequest request,
        CancellationToken cancellationToken = default);

    Task<Result<AiAnalyzeReportsResponseDto>> AnalyzeReportsAsync(
        AiAnalyzeReportsRequest request,
        CancellationToken cancellationToken = default);
}
