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

public sealed class RoleService : IRoleService
{
    private readonly QuestionnairesDbContext _db;
    private readonly IValidator<CreateRoleRequest> _createValidator;
    private readonly IValidator<UpdateRoleRequest> _updateValidator;
    private readonly IValidator<RoleFilterRequest> _filterValidator;

    public RoleService(
        QuestionnairesDbContext db,
        IValidator<CreateRoleRequest> createValidator,
        IValidator<UpdateRoleRequest> updateValidator,
        IValidator<RoleFilterRequest> filterValidator)
    {
        _db = db;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
        _filterValidator = filterValidator;
    }

    public async Task<Result<RoleDto>> CreateAsync(CreateRoleRequest request, CancellationToken cancellationToken = default)
    {
        var validation = await _createValidator.ValidateAsync(request, cancellationToken).ConfigureAwait(false);
        if (!validation.IsValid)
        {
            return Result<RoleDto>.Fail(validation.Errors.Select(e => e.ErrorMessage).ToList());
        }

        var nameAr = request.NameAr.Trim();
        var nameEn = request.NameEn.Trim();
        if (await _db.Roles.AsNoTracking()
                .AnyAsync(r => r.NameAr.ToLower() == nameAr.ToLower() || r.NameEn.ToLower() == nameEn.ToLower(), cancellationToken)
                .ConfigureAwait(false))
        {
            return Result<RoleDto>.Fail("A role with this name already exists.", IdentityErrors.DuplicateRoleName);
        }

        var role = new Role
        {
            NameAr = nameAr,
            NameEn = nameEn,
            DescriptionAr = string.IsNullOrWhiteSpace(request.DescriptionAr) ? null : request.DescriptionAr.Trim(),
            DescriptionEn = string.IsNullOrWhiteSpace(request.DescriptionEn) ? null : request.DescriptionEn.Trim(),
            IsSystemRole = request.IsSystemRole
        };

        _db.Roles.Add(role);
        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return Result<RoleDto>.Ok(await MapRoleDtoAsync(role.Id, cancellationToken).ConfigureAwait(false));
    }

    public async Task<Result<RoleDto>> UpdateAsync(Guid id, UpdateRoleRequest request, CancellationToken cancellationToken = default)
    {
        var validation = await _updateValidator.ValidateAsync(request, cancellationToken).ConfigureAwait(false);
        if (!validation.IsValid)
        {
            return Result<RoleDto>.Fail(validation.Errors.Select(e => e.ErrorMessage).ToList());
        }

        var role = await _db.Roles.FirstOrDefaultAsync(r => r.Id == id, cancellationToken).ConfigureAwait(false);
        if (role is null)
        {
            return Result<RoleDto>.Fail("The role was not found.", IdentityErrors.RoleNotFound);
        }

        var nameAr = request.NameAr.Trim();
        var nameEn = request.NameEn.Trim();
        if (role.IsSystemRole &&
            (!string.Equals(role.NameAr, nameAr, StringComparison.Ordinal) || !string.Equals(role.NameEn, nameEn, StringComparison.Ordinal)))
        {
            return Result<RoleDto>.Fail("System role names cannot be changed.", IdentityErrors.SystemRoleNameImmutable);
        }

        if (await _db.Roles.AsNoTracking()
                .AnyAsync(r =>
                    (r.NameAr.ToLower() == nameAr.ToLower() || r.NameEn.ToLower() == nameEn.ToLower()) && r.Id != id,
                    cancellationToken)
                .ConfigureAwait(false))
        {
            return Result<RoleDto>.Fail("A role with this name already exists.", IdentityErrors.DuplicateRoleName);
        }

        role.NameAr = nameAr;
        role.NameEn = nameEn;
        role.DescriptionAr = string.IsNullOrWhiteSpace(request.DescriptionAr) ? null : request.DescriptionAr.Trim();
        role.DescriptionEn = string.IsNullOrWhiteSpace(request.DescriptionEn) ? null : request.DescriptionEn.Trim();

        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return Result<RoleDto>.Ok(await MapRoleDtoAsync(role.Id, cancellationToken).ConfigureAwait(false));
    }

    public async Task<Result<RoleDto>> AssignPermissionsAsync(
        Guid id,
        AssignRolePermissionsRequest request,
        CancellationToken cancellationToken = default)
    {
        var role = await _db.Roles.FirstOrDefaultAsync(r => r.Id == id, cancellationToken).ConfigureAwait(false);
        if (role is null)
        {
            return Result<RoleDto>.Fail("The role was not found.", IdentityErrors.RoleNotFound);
        }

        var permissionIds = request.PermissionIds.Distinct().ToList();
        if (permissionIds.Count > 0)
        {
            var found = await _db.Permissions.AsNoTracking().Where(p => permissionIds.Contains(p.Id)).CountAsync(cancellationToken)
                .ConfigureAwait(false);
            if (found != permissionIds.Count)
            {
                return Result<RoleDto>.Fail("One or more permissions were not found.", IdentityErrors.PermissionNotFound);
            }
        }

        var existing = await _db.RolePermissions.Where(rp => rp.RoleId == id).ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        _db.RolePermissions.RemoveRange(existing);

        foreach (var permissionId in permissionIds)
        {
            _db.RolePermissions.Add(new RolePermission { RoleId = id, PermissionId = permissionId });
        }

        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return Result<RoleDto>.Ok(await MapRoleDtoAsync(id, cancellationToken).ConfigureAwait(false));
    }

    public async Task<Result<RoleDto>> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var exists = await _db.Roles.AsNoTracking().AnyAsync(r => r.Id == id, cancellationToken).ConfigureAwait(false);
        if (!exists)
        {
            return Result<RoleDto>.Fail("The role was not found.", IdentityErrors.RoleNotFound);
        }

        return Result<RoleDto>.Ok(await MapRoleDtoAsync(id, cancellationToken).ConfigureAwait(false));
    }

    public async Task<Result<PagedResult<RoleListItemDto>>> GetPagedAsync(
        RoleFilterRequest request,
        CancellationToken cancellationToken = default)
    {
        var validation = await _filterValidator.ValidateAsync(request, cancellationToken).ConfigureAwait(false);
        if (!validation.IsValid)
        {
            return Result<PagedResult<RoleListItemDto>>.Fail(validation.Errors.Select(e => e.ErrorMessage).ToList());
        }

        var page = request.Page <= 0 ? PaginationConstants.DefaultPage : request.Page;
        var pageSize = request.PageSize <= 0 ? PaginationConstants.DefaultPageSize : request.PageSize;
        if (pageSize > PaginationConstants.MaxPageSize)
        {
            pageSize = PaginationConstants.MaxPageSize;
        }

        var query = _db.Roles.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var term = request.Search.Trim();
            query = query.Where(r =>
                r.NameAr.Contains(term) ||
                r.NameEn.Contains(term) ||
                (r.DescriptionAr != null && r.DescriptionAr.Contains(term)) ||
                (r.DescriptionEn != null && r.DescriptionEn.Contains(term)));
        }

        var total = await query.CountAsync(cancellationToken).ConfigureAwait(false);
        var items = await query
            .OrderBy(r => r.NameEn)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(r => new RoleListItemDto
            {
                Id = r.Id,
                NameAr = r.NameAr,
                NameEn = r.NameEn,
                DescriptionAr = r.DescriptionAr,
                DescriptionEn = r.DescriptionEn,
                IsSystemRole = r.IsSystemRole
            })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return Result<PagedResult<RoleListItemDto>>.Ok(new PagedResult<RoleListItemDto>
        {
            Items = items,
            TotalCount = total,
            Page = page,
            PageSize = pageSize
        });
    }

    private async Task<RoleDto> MapRoleDtoAsync(Guid roleId, CancellationToken cancellationToken)
    {
        var role = await _db.Roles.AsNoTracking()
            .Where(r => r.Id == roleId)
            .Select(r => new
            {
                r.Id,
                r.NameAr,
                r.NameEn,
                r.DescriptionAr,
                r.DescriptionEn,
                r.IsSystemRole,
                PermissionCodes = r.RolePermissions
                    .Select(rp => rp.Permission.Code)
                    .OrderBy(c => c)
                    .ToList()
            })
            .FirstAsync(cancellationToken)
            .ConfigureAwait(false);

        return new RoleDto
        {
            Id = role.Id,
            NameAr = role.NameAr,
            NameEn = role.NameEn,
            DescriptionAr = role.DescriptionAr,
            DescriptionEn = role.DescriptionEn,
            IsSystemRole = role.IsSystemRole,
            PermissionCodes = role.PermissionCodes
        };
    }
}

