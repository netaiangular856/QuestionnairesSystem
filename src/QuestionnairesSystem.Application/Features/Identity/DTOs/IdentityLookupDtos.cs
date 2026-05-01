namespace QuestionnairesSystem.Application.Features.Identity.DTOs;

public sealed class LookupItemDto
{
    public Guid Id { get; init; }

    public string Name { get; init; } = string.Empty;

    public string? Email { get; init; }
}

public sealed class IdentityLookupsDto
{
    public IReadOnlyList<LookupItemDto> Employees { get; init; } = Array.Empty<LookupItemDto>();

    public IReadOnlyList<LookupItemDto> Users { get; init; } = Array.Empty<LookupItemDto>();

    public IReadOnlyList<LookupItemDto> Roles { get; init; } = Array.Empty<LookupItemDto>();

    public IReadOnlyList<LookupItemDto> Permissions { get; init; } = Array.Empty<LookupItemDto>();
}

