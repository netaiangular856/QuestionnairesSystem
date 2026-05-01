using QuestionnairesSystem.Shared.Abstractions;

namespace QuestionnairesSystem.Domain.Identity;

public sealed class Permission : AuditableEntity
{
    public string Code { get; set; } = null!;

    public string NameAr { get; set; } = null!;

    public string NameEn { get; set; } = null!;

    public string? DescriptionAr { get; set; }

    public string? DescriptionEn { get; set; }

    public string Module { get; set; } = null!;

    public ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();
}

