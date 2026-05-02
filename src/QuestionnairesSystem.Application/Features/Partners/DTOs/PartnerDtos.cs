using QuestionnairesSystem.Domain.Enums;

namespace QuestionnairesSystem.Application.Features.Partners.DTOs;

public sealed record PartnerDto(
    Guid Id,
    string Code,
    string NameAr,
    string NameEn,
    PartnerType Type,
    string? Email,
    string? PhoneNumber,
    string? ContactPerson,
    bool IsActive,
    string? Address);

public sealed record PartnerListItemDto(
    Guid Id,
    string Code,
    string NameAr,
    string NameEn,
    PartnerType Type,
    string? Email,
    bool IsActive);

public sealed record CreatePartnerRequest(
    string Code,
    string NameAr,
    string NameEn,
    PartnerType Type,
    string? Email,
    string? PhoneNumber,
    string? ContactPerson,
    string? Address);

public sealed record UpdatePartnerRequest(
    string Code,
    string NameAr,
    string NameEn,
    PartnerType Type,
    string? Email,
    string? PhoneNumber,
    string? ContactPerson,
    bool IsActive,
    string? Address);

public sealed record PartnerFilterRequest(
    string? Search,
    PartnerType? Type,
    bool? IsActive,
    int Page = 1,
    int PageSize = 10);
