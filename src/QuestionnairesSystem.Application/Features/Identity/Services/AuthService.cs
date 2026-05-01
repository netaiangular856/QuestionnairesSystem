using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using QuestionnairesSystem.Application.Common;
using QuestionnairesSystem.Application.Features.Identity.DTOs;
using QuestionnairesSystem.Application.Features.Identity.Interfaces;
using QuestionnairesSystem.Domain.Identity;
using QuestionnairesSystem.Persistence;
using QuestionnairesSystem.Shared.Results;

namespace QuestionnairesSystem.Application.Features.Identity.Services;

public sealed class AuthService : IAuthService
{
    private readonly ILogger<AuthService> _logger;
    private readonly QuestionnairesDbContext _db;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenIssuer _jwtTokenIssuer;
    private readonly IValidator<LoginRequest> _loginValidator;
    private readonly IValidator<RegisterRequest> _registerValidator;
    private readonly IValidator<ChangePasswordRequest> _changePasswordValidator;

    public AuthService(
        ILogger<AuthService> logger,
        QuestionnairesDbContext db,
        IPasswordHasher passwordHasher,
        IJwtTokenIssuer jwtTokenIssuer,
        IValidator<LoginRequest> loginValidator,
        IValidator<RegisterRequest> registerValidator,
        IValidator<ChangePasswordRequest> changePasswordValidator)
    {
        _logger = logger;
        _db = db;
        _passwordHasher = passwordHasher;
        _jwtTokenIssuer = jwtTokenIssuer;
        _loginValidator = loginValidator;
        _registerValidator = registerValidator;
        _changePasswordValidator = changePasswordValidator;
    }

    public async Task<Result<LoginResponseDto>> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        var validation = await _loginValidator.ValidateAsync(request, cancellationToken).ConfigureAwait(false);
        if (!validation.IsValid)
        {
            return Result<LoginResponseDto>.Fail(validation.Errors.Select(e => e.ErrorMessage).ToList());
        }

        var email = request.Email.Trim().ToLowerInvariant();

        var user = await _db.Users
            .Include(u => u.UserRoles)
            .ThenInclude(ur => ur.Role)
            .ThenInclude(r => r.RolePermissions)
            .ThenInclude(rp => rp.Permission)
            .FirstOrDefaultAsync(u => u.Email == email, cancellationToken)
            .ConfigureAwait(false);

        if (user is null || !_passwordHasher.Verify(request.Password, user.PasswordHash))
        {
            _logger.LogWarning("Login failed: invalid credentials or unknown account.");
            return Result<LoginResponseDto>.Fail("Invalid email or password.", IdentityErrors.InvalidCredentials);
        }

        if (!user.IsActive)
        {
            return Result<LoginResponseDto>.Fail("This account is inactive.", IdentityErrors.UserInactive);
        }

        return await IssueTokenAndUpdateLastLoginAsync(user, cancellationToken).ConfigureAwait(false);
    }

    public async Task<Result<LoginResponseDto>> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default)
    {
        var validation = await _registerValidator.ValidateAsync(request, cancellationToken).ConfigureAwait(false);
        if (!validation.IsValid)
        {
            return Result<LoginResponseDto>.Fail(validation.Errors.Select(e => e.ErrorMessage).ToList());
        }

        var userName = request.UserName.Trim().ToLowerInvariant();
        var email = request.Email.Trim().ToLowerInvariant();

        if (await _db.Users.AsNoTracking().AnyAsync(u => u.UserName == userName, cancellationToken).ConfigureAwait(false))
        {
            return Result<LoginResponseDto>.Fail("A user with this user name already exists.", IdentityErrors.DuplicateUserName);
        }

        if (await _db.Users.AsNoTracking().AnyAsync(u => u.Email == email, cancellationToken).ConfigureAwait(false))
        {
            return Result<LoginResponseDto>.Fail("A user with this email already exists.", IdentityErrors.DuplicateEmail);
        }

        var userRole = await _db.Roles.AsNoTracking()
            .FirstOrDefaultAsync(r => r.NameEn == SystemRoles.Employee, cancellationToken)
            .ConfigureAwait(false);

        if (userRole is null)
        {
            return Result<LoginResponseDto>.Fail("Default employee role is missing.", IdentityErrors.RoleNotFound);
        }

        var user = new User
        {
            UserName = userName,
            Email = email,
            NameAr = string.IsNullOrWhiteSpace(request.NameAr) ? null : request.NameAr.Trim(),
            NameEn = string.IsNullOrWhiteSpace(request.NameEn) ? null : request.NameEn.Trim(),
            PasswordHash = _passwordHasher.Hash(request.Password),
            IsActive = true
        };

        _db.Users.Add(user);
        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        _db.UserRoles.Add(new UserRole { UserId = user.Id, RoleId = userRole.Id });
        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        user = await _db.Users
            .Include(u => u.UserRoles)
            .ThenInclude(ur => ur.Role)
            .ThenInclude(r => r.RolePermissions)
            .ThenInclude(rp => rp.Permission)
            .FirstAsync(u => u.Id == user.Id, cancellationToken)
            .ConfigureAwait(false);

        return await IssueTokenAndUpdateLastLoginAsync(user, cancellationToken).ConfigureAwait(false);
    }

    public async Task<Result> ChangePasswordAsync(ChangePasswordRequest request, CancellationToken cancellationToken = default)
    {
        var validation = await _changePasswordValidator.ValidateAsync(request, cancellationToken).ConfigureAwait(false);
        if (!validation.IsValid)
        {
            return Result.Fail(validation.Errors.Select(e => e.ErrorMessage).ToList());
        }

        var email = request.Email.Trim().ToLowerInvariant();
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == email, cancellationToken).ConfigureAwait(false);
        if (user is null)
        {
            return Result.Fail("Invalid email or password.", IdentityErrors.InvalidCredentials);
        }

        if (!_passwordHasher.Verify(request.CurrentPassword, user.PasswordHash))
        {
            return Result.Fail("Current password is incorrect.", IdentityErrors.InvalidCredentials);
        }

        user.PasswordHash = _passwordHasher.Hash(request.NewPassword);
        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return Result.Ok();
    }

    private async Task<Result<LoginResponseDto>> IssueTokenAndUpdateLastLoginAsync(User user, CancellationToken cancellationToken)
    {
        var permissionCodes = user.UserRoles
            .SelectMany(ur => ur.Role.RolePermissions.Select(rp => rp.Permission.Code))
            .Distinct(StringComparer.Ordinal)
            .OrderBy(c => c, StringComparer.Ordinal)
            .ToList();

        var issued = _jwtTokenIssuer.IssueAccessToken(user.Id, user.UserName, user.Email, permissionCodes);

        user.LastLoginUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return Result<LoginResponseDto>.Ok(new LoginResponseDto
        {
            AccessToken = issued.Token,
            ExpiresAtUtc = issued.ExpiresAtUtc,
            TokenType = "Bearer"
        });
    }
}
