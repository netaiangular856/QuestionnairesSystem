using QuestionnairesSystem.Shared.Constants;

namespace QuestionnairesSystem.Application.Features.Identity.DTOs;

public sealed class RoleDto
{
    public Guid Id { get; init; }

    public string NameAr { get; init; } = string.Empty;

    public string NameEn { get; init; } = string.Empty;

    public string? DescriptionAr { get; init; }

    public string? DescriptionEn { get; init; }

    public bool IsSystemRole { get; init; }

    public IReadOnlyList<string> PermissionCodes { get; init; } = Array.Empty<string>();
}

public sealed class RoleListItemDto
{
    public Guid Id { get; init; }

    public string NameAr { get; init; } = string.Empty;

    public string NameEn { get; init; } = string.Empty;

    public string? DescriptionAr { get; init; }

    public string? DescriptionEn { get; init; }

    public bool IsSystemRole { get; init; }
}

public sealed class CreateRoleRequest
{
    public string NameAr { get; set; } = string.Empty;

    public string NameEn { get; set; } = string.Empty;

    public string? DescriptionAr { get; set; }

    public string? DescriptionEn { get; set; }

    public bool IsSystemRole { get; set; }
}

public sealed class UpdateRoleRequest
{
    public string NameAr { get; set; } = string.Empty;

    public string NameEn { get; set; } = string.Empty;

    public string? DescriptionAr { get; set; }

    public string? DescriptionEn { get; set; }
}

public sealed class AssignRolePermissionsRequest
{
    public IReadOnlyList<Guid> PermissionIds { get; set; } = Array.Empty<Guid>();
}

public sealed class RoleFilterRequest
{
    public int Page { get; set; } = PaginationConstants.DefaultPage;

    public int PageSize { get; set; } = PaginationConstants.DefaultPageSize;

    public string? Search { get; set; }
}

