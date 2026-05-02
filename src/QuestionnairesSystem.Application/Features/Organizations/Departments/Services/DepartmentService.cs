using Microsoft.EntityFrameworkCore;
using QuestionnairesSystem.Application.Features.Organizations.Departments.DTOs;
using QuestionnairesSystem.Application.Features.Organizations.Departments.Interfaces;
using QuestionnairesSystem.Domain.Organizations;
using QuestionnairesSystem.Persistence;
using QuestionnairesSystem.Shared.Results;
using QuestionnairesSystem.Shared.Api;

namespace QuestionnairesSystem.Application.Features.Organizations.Departments.Services;

public sealed class DepartmentService : IDepartmentService
{
    private readonly QuestionnairesDbContext _db;

    public DepartmentService(QuestionnairesDbContext db)
    {
        _db = db;
    }

    public async Task<Result<DepartmentDto>> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            var department = await _db.Departments
                .Include(x => x.ParentDepartment)
                .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

            if (department is null)
                return Result<DepartmentDto>.Fail("Department not found.");

            var employeeCount = await _db.Employees.CountAsync(x => x.DepartmentId == id, cancellationToken);
            var subDeptCount = await _db.Departments.CountAsync(x => x.ParentDepartmentId == id, cancellationToken);

            return Result<DepartmentDto>.Ok(new DepartmentDto(
                department.Id,
                department.Code,
                department.NameAr,
                department.NameEn,
                department.ParentDepartmentId,
                department.ParentDepartment?.NameAr,
                department.ParentDepartment?.NameEn,
                employeeCount,
                subDeptCount));
        }
        catch (OperationCanceledException)
        {
            return Result<DepartmentDto>.Fail("Operation was cancelled.");
        }
        catch (Exception ex)
        {
            return Result<DepartmentDto>.Fail($"Database error: {ex.Message}");
        }
    }

    public async Task<Result<PagedResult<DepartmentListItemDto>>> GetPagedListAsync(DepartmentFilterRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var query = _db.Departments
                .AsNoTracking();

            if (!string.IsNullOrWhiteSpace(request.Search))
            {
                query = query.Where(x => x.NameAr.Contains(request.Search) || 
                                       x.NameEn.Contains(request.Search) || 
                                       x.Code.Contains(request.Search));
            }

            if (request.ParentDepartmentId.HasValue)
            {
                query = query.Where(x => x.ParentDepartmentId == request.ParentDepartmentId);
            }

            var total = await query.CountAsync(cancellationToken);
            var items = await query
                .OrderBy(x => x.NameEn)
                .Skip((request.Page - 1) * request.PageSize)
                .Take(request.PageSize)
                .Select(x => new DepartmentListItemDto(
                    x.Id,
                    x.Code,
                    x.NameAr,
                    x.NameEn,
                    x.ParentDepartmentId,
                    x.ParentDepartment != null ? x.ParentDepartment.NameAr : null,
                    x.ParentDepartment != null ? x.ParentDepartment.NameEn : null,
                    _db.Employees.Count(e => e.DepartmentId == x.Id),
                    _db.Departments.Count(d => d.ParentDepartmentId == x.Id)))
                .ToListAsync(cancellationToken);

            return Result<PagedResult<DepartmentListItemDto>>.Ok(new PagedResult<DepartmentListItemDto>
            {
                Items = items,
                TotalCount = total,
                Page = request.Page,
                PageSize = request.PageSize
            });
        }
        catch (OperationCanceledException)
        {
            return Result<PagedResult<DepartmentListItemDto>>.Fail("Operation was cancelled.");
        }
        catch (Exception ex)
        {
            // Log the error here if you have a logger
            return Result<PagedResult<DepartmentListItemDto>>.Fail($"Database error: {ex.Message}");
        }
    }

    public async Task<Result<List<DepartmentTreeNodeDto>>> GetTreeAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var allDepartments = await _db.Departments
                .Include(x => x.Children)
                .AsNoTracking()
                .ToListAsync(cancellationToken);

            var employeeCounts = await _db.Employees
                .Where(x => x.DepartmentId != null)
                .GroupBy(x => x.DepartmentId)
                .Select(g => new { DepartmentId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.DepartmentId, x => x.Count, cancellationToken);

            var roots = allDepartments
                .Where(x => x.ParentDepartmentId == null)
                .Select(x => MapToTreeNode(x, allDepartments, employeeCounts))
                .ToList();

            return Result<List<DepartmentTreeNodeDto>>.Ok(roots);
        }
        catch (OperationCanceledException)
        {
            return Result<List<DepartmentTreeNodeDto>>.Fail("Operation was cancelled.");
        }
        catch (Exception ex)
        {
            return Result<List<DepartmentTreeNodeDto>>.Fail($"Database error: {ex.Message}");
        }
    }

    private DepartmentTreeNodeDto MapToTreeNode(Department dept, List<Department> all, Dictionary<Guid?, int> counts)
    {
        var count = counts.TryGetValue(dept.Id, out var c) ? c : 0;
        var children = all
            .Where(x => x.ParentDepartmentId == dept.Id)
            .Select(x => MapToTreeNode(x, all, counts))
            .ToList();

        return new DepartmentTreeNodeDto(dept.Id, dept.Code, dept.NameAr, dept.NameEn, count, children);
    }

    public async Task<Result<DepartmentDto>> CreateAsync(CreateDepartmentRequest request, CancellationToken cancellationToken = default)
    {
        if (await _db.Departments.AnyAsync(x => x.Code == request.Code, cancellationToken))
            return Result<DepartmentDto>.Fail("Department code already exists.");

        var department = new Department
        {
            Code = request.Code,
            NameAr = request.NameAr,
            NameEn = request.NameEn,
            ParentDepartmentId = request.ParentDepartmentId
        };

        _db.Departments.Add(department);
        await _db.SaveChangesAsync(cancellationToken);

        return await GetByIdAsync(department.Id, cancellationToken);
    }

    public async Task<Result<DepartmentDto>> UpdateAsync(Guid id, UpdateDepartmentRequest request, CancellationToken cancellationToken = default)
    {
        var department = await _db.Departments.FindAsync(new object[] { id }, cancellationToken);
        if (department is null)
            return Result<DepartmentDto>.Fail("Department not found.");

        if (await _db.Departments.AnyAsync(x => x.Code == request.Code && x.Id != id, cancellationToken))
            return Result<DepartmentDto>.Fail("Department code already exists.");

        if (request.ParentDepartmentId == id)
            return Result<DepartmentDto>.Fail("A department cannot be its own parent.");

        department.Code = request.Code;
        department.NameAr = request.NameAr;
        department.NameEn = request.NameEn;
        department.ParentDepartmentId = request.ParentDepartmentId;

        await _db.SaveChangesAsync(cancellationToken);

        return await GetByIdAsync(department.Id, cancellationToken);
    }

    public async Task<Result> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var department = await _db.Departments
            .Include(x => x.Children)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (department is null)
            return Result.Fail("Department not found.");

        if (department.Children.Any())
            return Result.Fail("Cannot delete a department that has sub-departments.");

        if (await _db.Employees.AnyAsync(x => x.DepartmentId == id, cancellationToken))
            return Result.Fail("Cannot delete a department that has employees.");

        _db.Departments.Remove(department);
        await _db.SaveChangesAsync(cancellationToken);

        return Result.Ok();
    }
}
