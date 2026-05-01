using QuestionnairesSystem.Application.Features.Questionnaires.Questions.DTOs;
using QuestionnairesSystem.Shared.Results;

namespace QuestionnairesSystem.Application.Features.Questionnaires.Questions.Interfaces;

public interface IQuestionnaireQuestionService
{
    Task<Result<QuestionDto>> CreateAsync(Guid surveyId, CreateQuestionRequest request, CancellationToken cancellationToken = default);
    Task<Result<IReadOnlyList<QuestionDto>>> ListBySurveyAsync(Guid surveyId, CancellationToken cancellationToken = default);
    Task<Result<QuestionDto>> GetByIdAsync(Guid questionId, CancellationToken cancellationToken = default);
    Task<Result<QuestionDto>> UpdateAsync(Guid questionId, UpdateQuestionRequest request, CancellationToken cancellationToken = default);
    Task<Result> SoftDeleteAsync(Guid questionId, CancellationToken cancellationToken = default);
    Task<Result> ReorderAsync(Guid surveyId, ReorderQuestionsRequest request, CancellationToken cancellationToken = default);
}
