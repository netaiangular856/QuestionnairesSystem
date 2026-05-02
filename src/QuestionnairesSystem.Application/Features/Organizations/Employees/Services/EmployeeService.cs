using Microsoft.EntityFrameworkCore;
using QuestionnairesSystem.Application.Features.Organizations.Employees.DTOs;
using QuestionnairesSystem.Application.Features.Organizations.Employees.Interfaces;
using QuestionnairesSystem.Domain.Organizations;
using QuestionnairesSystem.Persistence;
using QuestionnairesSystem.Shared.Results;
using QuestionnairesSystem.Shared.Api;

namespace QuestionnairesSystem.Application.Features.Organizations.Employees.Services;

public sealed class EmployeeService : IEmployeeService
{
    private readonly QuestionnairesDbContext _db;

    public EmployeeService(QuestionnairesDbContext db)
    {
        _db = db;
    }

    public async Task<Result<EmployeeDto>> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var employee = await _db.Employees
            .Include(x => x.Department)
            .Include(x => x.User)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (employee is null)
            return Result<EmployeeDto>.Fail("Employee not found.");

        return Result<EmployeeDto>.Ok(MapToDto(employee));
    }

    public async Task<Result<PagedResult<EmployeeListItemDto>>> GetPagedListAsync(EmployeeFilterRequest request, CancellationToken cancellationToken = default)
    {
        var query = _db.Employees
            .Include(x => x.Department)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var s = request.Search.Trim().ToLower();
            query = query.Where(x => x.NameAr.ToLower().Contains(s) || 
                                     x.NameEn.ToLower().Contains(s) || 
                                     x.EmployeeNumber.ToLower().Contains(s) ||
                                     (x.Email != null && x.Email.ToLower().Contains(s)));
        }

        if (request.DepartmentId.HasValue)
            query = query.Where(x => x.DepartmentId == request.DepartmentId);

        if (request.IsActive.HasValue)
            query = query.Where(x => x.IsActive == request.IsActive);

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderBy(x => x.EmployeeNumber)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(x => new EmployeeListItemDto(
                x.Id,
                x.EmployeeNumber,
                x.NameAr,
                x.NameEn,
                x.Email,
                x.JobTitleAr,
                x.JobTitleEn,
                x.Department != null ? x.Department.NameAr : null,
                x.Department != null ? x.Department.NameEn : null,
                x.IsActive))
            .ToListAsync(cancellationToken);

        return Result<PagedResult<EmployeeListItemDto>>.Ok(new PagedResult<EmployeeListItemDto>
        {
            Items = items,
            TotalCount = total,
            Page = request.Page,
            PageSize = request.PageSize
        });
    }

    public async Task<Result<EmployeeDto>> CreateAsync(CreateEmployeeRequest request, CancellationToken cancellationToken = default)
    {
        if (await _db.Employees.AnyAsync(x => x.EmployeeNumber == request.EmployeeNumber, cancellationToken))
            return Result<EmployeeDto>.Fail("Employee number already exists.");

        var employee = new Employee
        {
            EmployeeNumber = request.EmployeeNumber.Trim(),
            NameAr = request.NameAr.Trim(),
            NameEn = request.NameEn.Trim(),
            Email = request.Email?.Trim(),
            PhoneNumber = request.PhoneNumber?.Trim(),
            JobTitleAr = request.JobTitleAr?.Trim(),
            JobTitleEn = request.JobTitleEn?.Trim(),
            DepartmentId = request.DepartmentId
        };

        _db.Employees.Add(employee);
        await _db.SaveChangesAsync(cancellationToken);

        return await GetByIdAsync(employee.Id, cancellationToken);
    }

    public async Task<Result<EmployeeDto>> UpdateAsync(Guid id, UpdateEmployeeRequest request, CancellationToken cancellationToken = default)
    {
        var employee = await _db.Employees.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (employee is null)
            return Result<EmployeeDto>.Fail("Employee not found.");

        if (employee.EmployeeNumber != request.EmployeeNumber && 
            await _db.Employees.AnyAsync(x => x.EmployeeNumber == request.EmployeeNumber, cancellationToken))
            return Result<EmployeeDto>.Fail("Employee number already exists.");

        employee.EmployeeNumber = request.EmployeeNumber.Trim();
        employee.NameAr = request.NameAr.Trim();
        employee.NameEn = request.NameEn.Trim();
        employee.Email = request.Email?.Trim();
        employee.PhoneNumber = request.PhoneNumber?.Trim();
        employee.JobTitleAr = request.JobTitleAr?.Trim();
        employee.JobTitleEn = request.JobTitleEn?.Trim();
        employee.DepartmentId = request.DepartmentId;
        employee.IsActive = request.IsActive;

        await _db.SaveChangesAsync(cancellationToken);

        return await GetByIdAsync(employee.Id, cancellationToken);
    }

    public async Task<Result> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var employee = await _db.Employees.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (employee is null)
            return Result.Fail("Employee not found.");

        employee.RecordStatus = QuestionnairesSystem.Domain.Enums.RecordStatus.Deleted;
        await _db.SaveChangesAsync(cancellationToken);

        return Result.Ok();
    }

    private static EmployeeDto MapToDto(Employee x) => new(
        x.Id,
        x.EmployeeNumber,
        x.NameAr,
        x.NameEn,
        x.Email,
        x.PhoneNumber,
        x.JobTitleAr,
        x.JobTitleEn,
        x.DepartmentId,
        x.Department?.NameAr,
        x.Department?.NameEn,
        x.IsActive,
        x.User?.Id,
        x.User?.UserName);
}
