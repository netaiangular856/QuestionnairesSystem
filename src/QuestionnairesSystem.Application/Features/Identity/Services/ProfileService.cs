using FluentValidation;
using Microsoft.EntityFrameworkCore;
using QuestionnairesSystem.Application.Common;
using QuestionnairesSystem.Application.Features.Identity.DTOs;
using QuestionnairesSystem.Application.Features.Identity.Interfaces;
using QuestionnairesSystem.Persistence;
using QuestionnairesSystem.Shared.Identity;
using QuestionnairesSystem.Shared.Results;

namespace QuestionnairesSystem.Application.Features.Identity.Services;

public sealed class ProfileService : IProfileService
{
    private readonly QuestionnairesDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly IValidator<UpdateMyProfileRequest> _profileValidator;
    private readonly IUserService _userService;
    private readonly IAvatarStorage _avatarStorage;

    public ProfileService(
        QuestionnairesDbContext db,
        ICurrentUserService currentUser,
        IValidator<UpdateMyProfileRequest> profileValidator,
        IUserService userService,
        IAvatarStorage avatarStorage)
    {
        _db = db;
        _currentUser = currentUser;
        _profileValidator = profileValidator;
        _userService = userService;
        _avatarStorage = avatarStorage;
    }

    public Task<Result<UserDto>> GetMineAsync(CancellationToken cancellationToken = default)
    {
        var userId = _currentUser.UserId;
        if (userId is null)
        {
            return Task.FromResult(Result<UserDto>.Fail("Not authenticated.", IdentityErrors.InvalidCredentials));
        }

        return _userService.GetByIdAsync(userId.Value, cancellationToken);
    }

    public async Task<Result<UserDto>> UpdateMineAsync(UpdateMyProfileRequest request, CancellationToken cancellationToken = default)
    {
        var validation = await _profileValidator.ValidateAsync(request, cancellationToken).ConfigureAwait(false);
        if (!validation.IsValid)
        {
            return Result<UserDto>.Fail(validation.Errors.Select(e => e.ErrorMessage).ToList());
        }

        var userId = _currentUser.UserId;
        if (userId is null)
        {
            return Result<UserDto>.Fail("Not authenticated.", IdentityErrors.InvalidCredentials);
        }

        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId.Value, cancellationToken).ConfigureAwait(false);
        if (user is null)
        {
            return Result<UserDto>.Fail("The user was not found.", IdentityErrors.UserNotFound);
        }

        var email = NormalizeEmail(request.Email);
        if (await _db.Users.AsNoTracking().AnyAsync(u => u.Email == email && u.Id != userId.Value, cancellationToken)
                .ConfigureAwait(false))
        {
            return Result<UserDto>.Fail("A user with this email already exists.", IdentityErrors.DuplicateEmail);
        }

        user.NameAr = NormalizeOptionalName(request.NameAr);
        user.NameEn = NormalizeOptionalName(request.NameEn);
        user.Email = email;

        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return await _userService.GetByIdAsync(userId.Value, cancellationToken).ConfigureAwait(false);
    }

    public async Task<Result<UserDto>> SetAvatarFileNameAsync(string fileName, CancellationToken cancellationToken = default)
    {
        var userId = _currentUser.UserId;
        if (userId is null)
        {
            return Result<UserDto>.Fail("Not authenticated.", IdentityErrors.InvalidCredentials);
        }

        if (string.IsNullOrWhiteSpace(fileName))
        {
            return Result<UserDto>.Fail("Invalid file name.");
        }

        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId.Value, cancellationToken).ConfigureAwait(false);
        if (user is null)
        {
            return Result<UserDto>.Fail("The user was not found.", IdentityErrors.UserNotFound);
        }

        var previous = user.AvatarFileName;
        user.AvatarFileName = fileName.Trim();
        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        if (!string.Equals(previous, user.AvatarFileName, StringComparison.Ordinal))
        {
            _avatarStorage.TryDelete(previous);
        }

        return await _userService.GetByIdAsync(userId.Value, cancellationToken).ConfigureAwait(false);
    }

    public async Task<Result<UserDto>> ClearAvatarAsync(CancellationToken cancellationToken = default)
    {
        var userId = _currentUser.UserId;
        if (userId is null)
        {
            return Result<UserDto>.Fail("Not authenticated.", IdentityErrors.InvalidCredentials);
        }

        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId.Value, cancellationToken).ConfigureAwait(false);
        if (user is null)
        {
            return Result<UserDto>.Fail("The user was not found.", IdentityErrors.UserNotFound);
        }

        _avatarStorage.TryDelete(user.AvatarFileName);
        _avatarStorage.TryDeleteAllForUser(userId.Value);
        user.AvatarFileName = null;
        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return await _userService.GetByIdAsync(userId.Value, cancellationToken).ConfigureAwait(false);
    }

    private static string NormalizeEmail(string email) => email.Trim().ToLowerInvariant();

    private static string? NormalizeOptionalName(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return value.Trim();
    }
}
