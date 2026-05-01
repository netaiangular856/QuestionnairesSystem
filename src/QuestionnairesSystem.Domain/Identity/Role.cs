using QuestionnairesSystem.Shared.Abstractions;

namespace QuestionnairesSystem.Domain.Identity;

public sealed class Role : AuditableEntity
{
    public string NameAr { get; set; } = null!;

    public string NameEn { get; set; } = null!;

    public string? DescriptionAr { get; set; }

    public string? DescriptionEn { get; set; }

    public bool IsSystemRole { get; set; }

    public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();

    public ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();
}

