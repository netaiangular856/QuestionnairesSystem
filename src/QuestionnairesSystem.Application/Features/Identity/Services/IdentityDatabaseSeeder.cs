using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using QuestionnairesSystem.Application.Features.Identity.Interfaces;
using QuestionnairesSystem.Domain.Identity;
using QuestionnairesSystem.Persistence;
using QuestionnairesSystem.Shared.Options;

namespace QuestionnairesSystem.Application.Features.Identity.Services;

public sealed class IdentityDatabaseSeeder : IIdentityDatabaseSeeder
{
    private readonly QuestionnairesDbContext _db;
    private readonly IPermissionService _permissionService;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IdentitySeedOptions _seedOptions;

    public IdentityDatabaseSeeder(
        QuestionnairesDbContext db,
        IPermissionService permissionService,
        IPasswordHasher passwordHasher,
        IOptions<IdentitySeedOptions> seedOptions)
    {
        _db = db;
        _permissionService = permissionService;
        _passwordHasher = passwordHasher;
        _seedOptions = seedOptions.Value;
    }

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        await _permissionService.SeedDefaultPermissionsAsync(cancellationToken).ConfigureAwait(false);

        await EnsureSystemRoleAsync(SystemRoles.Admin, "مدير النظام", "Full system access", cancellationToken).ConfigureAwait(false);
        await EnsureSystemRoleAsync(SystemRoles.Hr, "الموارد البشرية", "Human resources", cancellationToken).ConfigureAwait(false);
        await EnsureSystemRoleAsync(SystemRoles.Manager, "المدير", "Line manager", cancellationToken).ConfigureAwait(false);
        await EnsureSystemRoleAsync(SystemRoles.Employee, "الموظف", "Standard employee", cancellationToken).ConfigureAwait(false);

        var adminRole = await _db.Roles.AsNoTracking()
            .FirstAsync(r => r.NameEn == SystemRoles.Admin, cancellationToken)
            .ConfigureAwait(false);

        var allPermissionIds = await _db.Permissions.AsNoTracking().Select(p => p.Id).ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var existingLinks = await _db.RolePermissions.AsNoTracking()
            .Where(rp => rp.RoleId == adminRole.Id)
            .Select(rp => rp.PermissionId)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var missing = allPermissionIds.Where(pid => !existingLinks.Contains(pid)).ToList();
        foreach (var pid in missing)
        {
            _db.RolePermissions.Add(new RolePermission { RoleId = adminRole.Id, PermissionId = pid });
        }

        if (missing.Count > 0)
        {
            await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }

        if (!_seedOptions.BootstrapAdmin)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(_seedOptions.AdminPassword))
        {
            throw new InvalidOperationException(
                "IdentitySeed:BootstrapAdmin is true but AdminPassword is empty. Set a strong password or disable BootstrapAdmin.");
        }

        var adminEmail = _seedOptions.AdminEmail.Trim().ToLowerInvariant();
        var adminUserName = _seedOptions.AdminUserName.Trim().ToLowerInvariant();

        if (await _db.Users.AsNoTracking().AnyAsync(u => u.Email == adminEmail, cancellationToken).ConfigureAwait(false))
        {
            return;
        }

        var user = new User
        {
            UserName = adminUserName,
            Email = adminEmail,
            PasswordHash = _passwordHasher.Hash(_seedOptions.AdminPassword),
            IsActive = true,
            EmployeeId = null
        };

        _db.Users.Add(user);
        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        _db.UserRoles.Add(new UserRole { UserId = user.Id, RoleId = adminRole.Id });
        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    private async Task EnsureSystemRoleAsync(string nameEn, string nameAr, string descriptionEn, CancellationToken cancellationToken)
    {
        if (await _db.Roles.AsNoTracking().AnyAsync(r => r.NameEn == nameEn, cancellationToken).ConfigureAwait(false))
        {
            return;
        }

        _db.Roles.Add(new Role
        {
            NameAr = nameAr,
            NameEn = nameEn,
            DescriptionAr = descriptionEn,
            DescriptionEn = descriptionEn,
            IsSystemRole = true
        });

        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}

