using QuestionnairesSystem.Domain.Enums;

namespace QuestionnairesSystem.Application.Features.Organizations.Employees.DTOs;

public sealed record EmployeeDto(
    Guid Id,
    string EmployeeNumber,
    string NameAr,
    string NameEn,
    string? Email,
    string? PhoneNumber,
    string? JobTitleAr,
    string? JobTitleEn,
    Guid? DepartmentId,
    string? DepartmentNameAr,
    string? DepartmentNameEn,
    bool IsActive,
    Guid? UserId,
    string? UserName);

public sealed record EmployeeListItemDto(
    Guid Id,
    string EmployeeNumber,
    string NameAr,
    string NameEn,
    string? Email,
    string? JobTitleAr,
    string? JobTitleEn,
    string? DepartmentNameAr,
    string? DepartmentNameEn,
    bool IsActive);

public sealed record CreateEmployeeRequest(
    string EmployeeNumber,
    string NameAr,
    string NameEn,
    string? Email,
    string? PhoneNumber,
    string? JobTitleAr,
    string? JobTitleEn,
    Guid? DepartmentId);

public sealed record UpdateEmployeeRequest(
    string EmployeeNumber,
    string NameAr,
    string NameEn,
    string? Email,
    string? PhoneNumber,
    string? JobTitleAr,
    string? JobTitleEn,
    Guid? DepartmentId,
    bool IsActive);

public sealed record EmployeeFilterRequest(
    string? Search,
    Guid? DepartmentId,
    bool? IsActive,
    int Page = 1,
    int PageSize = 10);
