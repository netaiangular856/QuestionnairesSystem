using System.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using QuestionnairesSystem.Api.Extensions;
using QuestionnairesSystem.Application.Features.Identity.DTOs;
using QuestionnairesSystem.Application.Features.Identity.Interfaces;
using QuestionnairesSystem.Shared.Api;

namespace QuestionnairesSystem.Api.Controllers;

[ApiController]
[Route("api/account")]
public sealed class AccountController : ControllerBase
{
    private readonly IProfileService _profileService;
    private readonly IAvatarStorage _avatarStorage;

    public AccountController(IProfileService profileService, IAvatarStorage avatarStorage)
    {
        _profileService = profileService;
        _avatarStorage = avatarStorage;
    }

    [HttpGet("profile")]
    [ProducesResponseType(typeof(ApiResponse<UserDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetProfile(CancellationToken cancellationToken)
    {
        var traceId = Activity.Current?.Id ?? HttpContext.TraceIdentifier;
        var result = await _profileService.GetMineAsync(cancellationToken);
        return result.ToApiActionResult(this, traceId);
    }

    [HttpPut("profile")]
    [ProducesResponseType(typeof(ApiResponse<UserDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateProfile(
        [FromBody] UpdateMyProfileRequest request,
        CancellationToken cancellationToken)
    {
        var traceId = Activity.Current?.Id ?? HttpContext.TraceIdentifier;
        var result = await _profileService.UpdateMineAsync(request, cancellationToken);
        return result.ToApiActionResult(this, traceId);
    }

    [HttpPost("profile/avatar")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(10 * 1024 * 1024)]
    [RequestFormLimits(MultipartBodyLengthLimit = 10 * 1024 * 1024)]
    [ProducesResponseType(typeof(ApiResponse<UserDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> UploadAvatar(IFormFile? file, CancellationToken cancellationToken)
    {
        var traceId = Activity.Current?.Id ?? HttpContext.TraceIdentifier;
        if (file is null || file.Length == 0)
        {
            return BadRequest(ApiResponse<UserDto>.FromFailure(new[] { "No file was uploaded." }, traceId));
        }

        var userId = GetRequiredUserId();
        if (userId is null)
        {
            return Unauthorized(ApiResponse<UserDto>.FromFailure(new[] { "Not authenticated." }, traceId));
        }

        var ext = Path.GetExtension(file.FileName);
        try
        {
            await using var stream = file.OpenReadStream();
            var storedName = await _avatarStorage.SaveAsync(userId.Value, stream, ext, cancellationToken)
                .ConfigureAwait(false);
            var result = await _profileService.SetAvatarFileNameAsync(storedName, cancellationToken)
                .ConfigureAwait(false);
            return result.ToApiActionResult(this, traceId);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<UserDto>.FromFailure(new[] { ex.Message }, traceId));
        }
    }

    [HttpDelete("profile/avatar")]
    [ProducesResponseType(typeof(ApiResponse<UserDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ClearAvatar(CancellationToken cancellationToken)
    {
        var traceId = Activity.Current?.Id ?? HttpContext.TraceIdentifier;
        var result = await _profileService.ClearAvatarAsync(cancellationToken);
        return result.ToApiActionResult(this, traceId);
    }

    private Guid? GetRequiredUserId()
    {
        var value = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
            ?? User.FindFirst("sub")?.Value;
        return Guid.TryParse(value, out var id) ? id : null;
    }
}
