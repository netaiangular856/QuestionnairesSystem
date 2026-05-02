using QuestionnairesSystem.Application.Features.Partners.DTOs;
using QuestionnairesSystem.Shared.Results;
using QuestionnairesSystem.Shared.Api;

namespace QuestionnairesSystem.Application.Features.Partners.Interfaces;

public interface IPartnerService
{
    Task<Result<PartnerDto>> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Result<PagedResult<PartnerListItemDto>>> GetPagedListAsync(PartnerFilterRequest request, CancellationToken cancellationToken = default);
    Task<Result<PartnerDto>> CreateAsync(CreatePartnerRequest request, CancellationToken cancellationToken = default);
    Task<Result<PartnerDto>> UpdateAsync(Guid id, UpdatePartnerRequest request, CancellationToken cancellationToken = default);
    Task<Result> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
