using FluentValidation;
using Microsoft.EntityFrameworkCore;
using QuestionnairesSystem.Application.Common;
using QuestionnairesSystem.Application.Features.Identity.DTOs;
using QuestionnairesSystem.Application.Features.Identity.Interfaces;
using QuestionnairesSystem.Domain.Identity;
using QuestionnairesSystem.Persistence;
using QuestionnairesSystem.Shared.Api;
using QuestionnairesSystem.Shared.Constants;
using QuestionnairesSystem.Shared.Results;

namespace QuestionnairesSystem.Application.Features.Identity.Services;

public sealed class UserService : IUserService
{
    private readonly QuestionnairesDbContext _db;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IValidator<CreateUserRequest> _createValidator;
    private readonly IValidator<UpdateUserRequest> _updateValidator;
    private readonly IValidator<UserFilterRequest> _filterValidator;

    public UserService(
        QuestionnairesDbContext db,
        IPasswordHasher passwordHasher,
        IValidator<CreateUserRequest> createValidator,
        IValidator<UpdateUserRequest> updateValidator,
        IValidator<UserFilterRequest> filterValidator)
    {
        _db = db;
        _passwordHasher = passwordHasher;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
        _filterValidator = filterValidator;
    }

    public async Task<Result<UserDto>> CreateAsync(CreateUserRequest request, CancellationToken cancellationToken = default)
    {
        var validation = await _createValidator.ValidateAsync(request, cancellationToken).ConfigureAwait(false);
        if (!validation.IsValid)
        {
            return Result<UserDto>.Fail(validation.Errors.Select(e => e.ErrorMessage).ToList());
        }

        var userName = NormalizeUserName(request.UserName);
        var email = NormalizeEmail(request.Email);

        if (await _db.Users.AsNoTracking().AnyAsync(u => u.UserName == userName, cancellationToken).ConfigureAwait(false))
        {
            return Result<UserDto>.Fail("A user with this user name already exists.", IdentityErrors.DuplicateUserName);
        }

        if (await _db.Users.AsNoTracking().AnyAsync(u => u.Email == email, cancellationToken).ConfigureAwait(false))
        {
            return Result<UserDto>.Fail("A user with this email already exists.", IdentityErrors.DuplicateEmail);
        }

        var roleIds = request.RoleIds.Distinct().ToList();
        if (roleIds.Count > 0)
        {
            var found = await _db.Roles.AsNoTracking().Where(r => roleIds.Contains(r.Id)).CountAsync(cancellationToken)
                .ConfigureAwait(false);
            if (found != roleIds.Count)
            {
                return Result<UserDto>.Fail("One or more roles were not found.", IdentityErrors.RoleNotFound);
            }
        }

        var user = new User
        {
            UserName = userName,
            NameAr = NormalizeOptionalName(request.NameAr),
            NameEn = NormalizeOptionalName(request.NameEn),
            Email = email,
            PasswordHash = _passwordHasher.Hash(request.Password),
            IsActive = true,
            EmployeeId = request.EmployeeId
        };

        _db.Users.Add(user);
        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        foreach (var roleId in roleIds)
        {
            _db.UserRoles.Add(new UserRole { UserId = user.Id, RoleId = roleId });
        }

        if (roleIds.Count > 0)
        {
            await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }

        return Result<UserDto>.Ok(await MapUserDtoAsync(user.Id, cancellationToken).ConfigureAwait(false));
    }

    public async Task<Result<UserDto>> UpdateAsync(Guid id, UpdateUserRequest request, CancellationToken cancellationToken = default)
    {
        var validation = await _updateValidator.ValidateAsync(request, cancellationToken).ConfigureAwait(false);
        if (!validation.IsValid)
        {
            return Result<UserDto>.Fail(validation.Errors.Select(e => e.ErrorMessage).ToList());
        }

        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == id, cancellationToken).ConfigureAwait(false);
        if (user is null)
        {
            return Result<UserDto>.Fail("The user was not found.", IdentityErrors.UserNotFound);
        }

        var userName = NormalizeUserName(request.UserName);
        var email = NormalizeEmail(request.Email);

        if (await _db.Users.AsNoTracking().AnyAsync(u => u.UserName == userName && u.Id != id, cancellationToken)
                .ConfigureAwait(false))
        {
            return Result<UserDto>.Fail("A user with this user name already exists.", IdentityErrors.DuplicateUserName);
        }

        if (await _db.Users.AsNoTracking().AnyAsync(u => u.Email == email && u.Id != id, cancellationToken)
                .ConfigureAwait(false))
        {
            return Result<UserDto>.Fail("A user with this email already exists.", IdentityErrors.DuplicateEmail);
        }

        user.UserName = userName;
        user.NameAr = NormalizeOptionalName(request.NameAr);
        user.NameEn = NormalizeOptionalName(request.NameEn);
        user.Email = email;
        user.EmployeeId = request.EmployeeId;

        if (!string.IsNullOrWhiteSpace(request.NewPassword))
        {
            user.PasswordHash = _passwordHasher.Hash(request.NewPassword!);
        }

        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return Result<UserDto>.Ok(await MapUserDtoAsync(user.Id, cancellationToken).ConfigureAwait(false));
    }

    public async Task<Result> ActivateAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == id, cancellationToken).ConfigureAwait(false);
        if (user is null)
        {
            return Result.Fail("The user was not found.", IdentityErrors.UserNotFound);
        }

        user.IsActive = true;
        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Result.Ok();
    }

    public async Task<Result> DeactivateAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == id, cancellationToken).ConfigureAwait(false);
        if (user is null)
        {
            return Result.Fail("The user was not found.", IdentityErrors.UserNotFound);
        }

        user.IsActive = false;
        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Result.Ok();
    }

    public async Task<Result<UserDto>> AssignRolesAsync(Guid id, AssignUserRolesRequest request, CancellationToken cancellationToken = default)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == id, cancellationToken).ConfigureAwait(false);
        if (user is null)
        {
            return Result<UserDto>.Fail("The user was not found.", IdentityErrors.UserNotFound);
        }

        var roleIds = request.RoleIds.Distinct().ToList();
        if (roleIds.Count > 0)
        {
            var found = await _db.Roles.AsNoTracking().Where(r => roleIds.Contains(r.Id)).CountAsync(cancellationToken)
                .ConfigureAwait(false);
            if (found != roleIds.Count)
            {
                return Result<UserDto>.Fail("One or more roles were not found.", IdentityErrors.RoleNotFound);
            }
        }

        var existing = await _db.UserRoles.Where(ur => ur.UserId == id).ToListAsync(cancellationToken).ConfigureAwait(false);
        _db.UserRoles.RemoveRange(existing);

        foreach (var roleId in roleIds)
        {
            _db.UserRoles.Add(new UserRole { UserId = id, RoleId = roleId });
        }

        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return Result<UserDto>.Ok(await MapUserDtoAsync(id, cancellationToken).ConfigureAwait(false));
    }

    public async Task<Result<UserDto>> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var exists = await _db.Users.AsNoTracking().AnyAsync(u => u.Id == id, cancellationToken).ConfigureAwait(false);
        if (!exists)
        {
            return Result<UserDto>.Fail("The user was not found.", IdentityErrors.UserNotFound);
        }

        return Result<UserDto>.Ok(await MapUserDtoAsync(id, cancellationToken).ConfigureAwait(false));
    }

    public async Task<Result<PagedResult<UserListItemDto>>> GetPagedAsync(
        UserFilterRequest request,
        CancellationToken cancellationToken = default)
    {
        var validation = await _filterValidator.ValidateAsync(request, cancellationToken).ConfigureAwait(false);
        if (!validation.IsValid)
        {
            return Result<PagedResult<UserListItemDto>>.Fail(validation.Errors.Select(e => e.ErrorMessage).ToList());
        }

        var page = request.Page <= 0 ? PaginationConstants.DefaultPage : request.Page;
        var pageSize = request.PageSize <= 0 ? PaginationConstants.DefaultPageSize : request.PageSize;
        if (pageSize > PaginationConstants.MaxPageSize)
        {
            pageSize = PaginationConstants.MaxPageSize;
        }

        var query = _db.Users.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var term = request.Search.Trim();
            query = query.Where(u =>
                u.UserName.Contains(term) ||
                u.Email.Contains(term) ||
                (u.NameAr != null && u.NameAr.Contains(term)) ||
                (u.NameEn != null && u.NameEn.Contains(term)));
        }

        var total = await query.CountAsync(cancellationToken).ConfigureAwait(false);
        var items = await query
            .OrderBy(u => u.UserName)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(u => new UserListItemDto
            {
                Id = u.Id,
                UserName = u.UserName,
                NameAr = u.NameAr,
                NameEn = u.NameEn,
                Email = u.Email,
                IsActive = u.IsActive,
                EmployeeId = u.EmployeeId,
                LastLoginUtc = u.LastLoginUtc
            })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return Result<PagedResult<UserListItemDto>>.Ok(new PagedResult<UserListItemDto>
        {
            Items = items,
            TotalCount = total,
            Page = page,
            PageSize = pageSize
        });
    }

    private async Task<UserDto> MapUserDtoAsync(Guid userId, CancellationToken cancellationToken)
    {
        var user = await _db.Users.AsNoTracking()
            .Where(u => u.Id == userId)
            .Select(u => new
            {
                u.Id,
                u.UserName,
                u.NameAr,
                u.NameEn,
                u.Email,
                u.IsActive,
                u.EmployeeId,
                u.LastLoginUtc,
                u.AvatarFileName,
                RoleNames = u.UserRoles
                    .Select(ur => string.IsNullOrWhiteSpace(ur.Role.NameAr) ? ur.Role.NameEn : ur.Role.NameAr)
                    .OrderBy(n => n)
                    .ToList()
            })
            .FirstAsync(cancellationToken)
            .ConfigureAwait(false);

        return new UserDto
        {
            Id = user.Id,
            UserName = user.UserName,
            NameAr = user.NameAr,
            NameEn = user.NameEn,
            Email = user.Email,
            IsActive = user.IsActive,
            EmployeeId = user.EmployeeId,
            LastLoginUtc = user.LastLoginUtc,
            AvatarUrl = AvatarUrls.ToPublicUrl(user.AvatarFileName),
            RoleNames = user.RoleNames
        };
    }

    private static string NormalizeUserName(string userName) => userName.Trim().ToLowerInvariant();

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

