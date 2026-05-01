namespace QuestionnairesSystem.Application.Features.Identity.DTOs;

public sealed class PermissionDto
{
    public Guid Id { get; init; }

    public string Code { get; init; } = string.Empty;

    public string NameAr { get; init; } = string.Empty;

    public string NameEn { get; init; } = string.Empty;

    public string? DescriptionAr { get; init; }

    public string? DescriptionEn { get; init; }

    public string Module { get; init; } = string.Empty;
}

