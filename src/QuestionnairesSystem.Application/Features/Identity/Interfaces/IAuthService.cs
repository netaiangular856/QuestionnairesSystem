using QuestionnairesSystem.Application.Features.Identity.DTOs;
using QuestionnairesSystem.Shared.Results;

namespace QuestionnairesSystem.Application.Features.Identity.Interfaces;

public interface IAuthService
{
    Task<Result<LoginResponseDto>> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default);
    Task<Result<LoginResponseDto>> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default);
    Task<Result> ChangePasswordAsync(ChangePasswordRequest request, CancellationToken cancellationToken = default);
}
