using QuestionnairesSystem.Application.Features.Questionnaires.Templates.DTOs;
using QuestionnairesSystem.Shared.Results;

namespace QuestionnairesSystem.Application.Features.Questionnaires.Templates.Interfaces;

public interface ISurveyTemplateService
{
    Task<Result<TemplateDetailDto>> CreateAsync(CreateTemplateRequest request, CancellationToken cancellationToken = default);
    Task<Result<IReadOnlyList<TemplateListItemDto>>> ListAsync(bool includeArchived, CancellationToken cancellationToken = default);
    Task<Result<TemplateDetailDto>> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Result<TemplateDetailDto>> UpdateAsync(Guid id, UpdateTemplateRequest request, CancellationToken cancellationToken = default);
    Task<Result> SoftDeleteAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Result<UseTemplateResultDto>> UseAsync(Guid templateId, CancellationToken cancellationToken = default);
    Task<Result<TemplateDetailDto>> ArchiveAsync(Guid id, CancellationToken cancellationToken = default);
}
