using Microsoft.EntityFrameworkCore;
using QuestionnairesSystem.Application.Features.Identity.DTOs;
using QuestionnairesSystem.Application.Features.Identity.Interfaces;
using QuestionnairesSystem.Domain.Identity;
using QuestionnairesSystem.Persistence;
using QuestionnairesSystem.Shared.Results;

namespace QuestionnairesSystem.Application.Features.Identity.Services;

public sealed class PermissionService : IPermissionService
{
    private readonly QuestionnairesDbContext _db;

    public PermissionService(QuestionnairesDbContext db)
    {
        _db = db;
    }

    public async Task<Result> SeedDefaultPermissionsAsync(CancellationToken cancellationToken = default)
    {
        var existingByCode = await _db.Permissions
            .ToDictionaryAsync(p => p.Code, cancellationToken)
            .ConfigureAwait(false);

        var hasChanges = false;
        foreach (var def in PermissionDefinitions.All)
        {
            if (existingByCode.TryGetValue(def.Code, out var existing))
            {
                // Keep seeded permissions synchronized with latest bilingual labels.
                if (!string.Equals(existing.NameAr, def.NameAr, StringComparison.Ordinal) ||
                    !string.Equals(existing.NameEn, def.NameEn, StringComparison.Ordinal) ||
                    !string.Equals(existing.DescriptionAr, def.DescriptionAr, StringComparison.Ordinal) ||
                    !string.Equals(existing.DescriptionEn, def.DescriptionEn, StringComparison.Ordinal) ||
                    !string.Equals(existing.Module, def.Module, StringComparison.Ordinal))
                {
                    existing.NameAr = def.NameAr;
                    existing.NameEn = def.NameEn;
                    existing.DescriptionAr = def.DescriptionAr;
                    existing.DescriptionEn = def.DescriptionEn;
                    existing.Module = def.Module;
                    hasChanges = true;
                }

                continue;
            }

            _db.Permissions.Add(new Permission
            {
                Code = def.Code,
                NameAr = def.NameAr,
                NameEn = def.NameEn,
                DescriptionAr = def.DescriptionAr,
                DescriptionEn = def.DescriptionEn,
                Module = def.Module
            });
            hasChanges = true;
        }

        if (hasChanges)
        {
            await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }

        return Result.Ok();
    }

    public async Task<Result<IReadOnlyList<PermissionDto>>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var list = await _db.Permissions.AsNoTracking()
            .OrderBy(p => p.Module)
            .ThenBy(p => p.Code)
            .Select(p => new PermissionDto
            {
                Id = p.Id,
                Code = p.Code,
                NameAr = p.NameAr,
                NameEn = p.NameEn,
                DescriptionAr = p.DescriptionAr,
                DescriptionEn = p.DescriptionEn,
                Module = p.Module
            })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return Result<IReadOnlyList<PermissionDto>>.Ok(list);
    }
}

