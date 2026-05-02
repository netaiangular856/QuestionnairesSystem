namespace QuestionnairesSystem.Application.Features.Organizations.Departments.DTOs;

public sealed record DepartmentDto(
    Guid Id,
    string Code,
    string NameAr,
    string NameEn,
    Guid? ParentDepartmentId,
    string? ParentDepartmentNameAr,
    string? ParentDepartmentNameEn,
    int EmployeeCount,
    int SubDepartmentCount);

public sealed record DepartmentListItemDto(
    Guid Id,
    string Code,
    string NameAr,
    string NameEn,
    Guid? ParentDepartmentId,
    string? ParentDepartmentNameAr,
    string? ParentDepartmentNameEn,
    int EmployeeCount,
    int SubDepartmentCount);

public sealed record DepartmentTreeNodeDto(
    Guid Id,
    string Code,
    string NameAr,
    string NameEn,
    int EmployeeCount,
    List<DepartmentTreeNodeDto> Children);

public sealed record CreateDepartmentRequest(
    string Code,
    string NameAr,
    string NameEn,
    Guid? ParentDepartmentId);

public sealed record UpdateDepartmentRequest(
    string Code,
    string NameAr,
    string NameEn,
    Guid? ParentDepartmentId);

public sealed record DepartmentFilterRequest(
    string? Search,
    Guid? ParentDepartmentId,
    int Page = 1,
    int PageSize = 10);
