using QuestionnairesSystem.Application.Features.Identity.DTOs;
using QuestionnairesSystem.Shared.Results;

namespace QuestionnairesSystem.Application.Features.Identity.Interfaces;

public interface IProfileService
{
    Task<Result<UserDto>> GetMineAsync(CancellationToken cancellationToken = default);

    Task<Result<UserDto>> UpdateMineAsync(UpdateMyProfileRequest request, CancellationToken cancellationToken = default);

    Task<Result<UserDto>> SetAvatarFileNameAsync(string fileName, CancellationToken cancellationToken = default);

    Task<Result<UserDto>> ClearAvatarAsync(CancellationToken cancellationToken = default);
}
