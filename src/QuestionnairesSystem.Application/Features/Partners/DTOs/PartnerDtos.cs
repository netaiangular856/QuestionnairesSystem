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
    string? Address,
    Guid? DepartmentId,
    string? DepartmentNameAr,
    string? DepartmentNameEn);

public sealed record PartnerListItemDto(
    Guid Id,
    string Code,
    string NameAr,
    string NameEn,
    PartnerType Type,
    string? Email,
    bool IsActive,
    string? DepartmentNameAr,
    string? DepartmentNameEn);

public sealed record CreatePartnerRequest(
    string Code,
    string NameAr,
    string NameEn,
    PartnerType Type,
    string? Email,
    string? PhoneNumber,
    string? ContactPerson,
    string? Address,
    Guid? DepartmentId);

public sealed record UpdatePartnerRequest(
    string Code,
    string NameAr,
    string NameEn,
    PartnerType Type,
    string? Email,
    string? PhoneNumber,
    string? ContactPerson,
    bool IsActive,
    string? Address,
    Guid? DepartmentId);

public sealed record PartnerFilterRequest(
    string? Search,
    PartnerType? Type,
    bool? IsActive,
    int Page = 1,
    int PageSize = 10);
