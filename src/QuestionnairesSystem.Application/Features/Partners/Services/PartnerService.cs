using Microsoft.EntityFrameworkCore;
using QuestionnairesSystem.Application.Features.Partners.DTOs;
using QuestionnairesSystem.Application.Features.Partners.Interfaces;
using QuestionnairesSystem.Domain.Partners;
using QuestionnairesSystem.Persistence;
using QuestionnairesSystem.Shared.Results;
using QuestionnairesSystem.Shared.Api;

namespace QuestionnairesSystem.Application.Features.Partners.Services;

public sealed class PartnerService : IPartnerService
{
    private readonly QuestionnairesDbContext _db;

    public PartnerService(QuestionnairesDbContext db)
    {
        _db = db;
    }

    public async Task<Result<PartnerDto>> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var partner = await _db.Partners
            .Include(x => x.Department)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (partner is null)
            return Result<PartnerDto>.Fail("Partner not found.");

        return Result<PartnerDto>.Ok(MapToDto(partner));
    }

    public async Task<Result<PagedResult<PartnerListItemDto>>> GetPagedListAsync(PartnerFilterRequest request, CancellationToken cancellationToken = default)
    {
        var query = _db.Partners
            .Include(x => x.Department)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var s = request.Search.Trim().ToLower();
            query = query.Where(x => x.NameAr.ToLower().Contains(s) || 
                                     x.NameEn.ToLower().Contains(s) || 
                                     x.Code.ToLower().Contains(s) ||
                                     (x.Email != null && x.Email.ToLower().Contains(s)));
        }

        if (request.Type.HasValue)
            query = query.Where(x => x.Type == request.Type);

        if (request.IsActive.HasValue)
            query = query.Where(x => x.IsActive == request.IsActive);

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderBy(x => x.Code)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(x => new PartnerListItemDto(
                x.Id,
                x.Code,
                x.NameAr,
                x.NameEn,
                x.Type,
                x.Email,
                x.IsActive,
                x.Department != null ? x.Department.NameAr : null,
                x.Department != null ? x.Department.NameEn : null))
            .ToListAsync(cancellationToken);

        return Result<PagedResult<PartnerListItemDto>>.Ok(new PagedResult<PartnerListItemDto>
        {
            Items = items,
            TotalCount = total,
            Page = request.Page,
            PageSize = request.PageSize
        });
    }

    public async Task<Result<PartnerDto>> CreateAsync(CreatePartnerRequest request, CancellationToken cancellationToken = default)
    {
        if (await _db.Partners.AnyAsync(x => x.Code == request.Code, cancellationToken))
            return Result<PartnerDto>.Fail("Partner code already exists.");

        var partner = new Partner
        {
            Code = request.Code.Trim(),
            NameAr = request.NameAr.Trim(),
            NameEn = request.NameEn.Trim(),
            Type = request.Type,
            Email = request.Email?.Trim(),
            PhoneNumber = request.PhoneNumber?.Trim(),
            ContactPerson = request.ContactPerson?.Trim(),
            Address = request.Address?.Trim(),
            DepartmentId = request.DepartmentId
        };

        _db.Partners.Add(partner);
        await _db.SaveChangesAsync(cancellationToken);

        return await GetByIdAsync(partner.Id, cancellationToken);
    }

    public async Task<Result<PartnerDto>> UpdateAsync(Guid id, UpdatePartnerRequest request, CancellationToken cancellationToken = default)
    {
        var partner = await _db.Partners.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (partner is null)
            return Result<PartnerDto>.Fail("Partner not found.");

        if (partner.Code != request.Code && 
            await _db.Partners.AnyAsync(x => x.Code == request.Code, cancellationToken))
            return Result<PartnerDto>.Fail("Partner code already exists.");

        partner.Code = request.Code.Trim();
        partner.NameAr = request.NameAr.Trim();
        partner.NameEn = request.NameEn.Trim();
        partner.Type = request.Type;
        partner.Email = request.Email?.Trim();
        partner.PhoneNumber = request.PhoneNumber?.Trim();
        partner.ContactPerson = request.ContactPerson?.Trim();
        partner.Address = request.Address?.Trim();
        partner.IsActive = request.IsActive;
        partner.DepartmentId = request.DepartmentId;

        await _db.SaveChangesAsync(cancellationToken);

        return await GetByIdAsync(partner.Id, cancellationToken);
    }

    public async Task<Result> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var partner = await _db.Partners.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (partner is null)
            return Result.Fail("Partner not found.");

        partner.RecordStatus = QuestionnairesSystem.Domain.Enums.RecordStatus.Deleted;
        await _db.SaveChangesAsync(cancellationToken);

        return Result.Ok();
    }

    private static PartnerDto MapToDto(Partner x) => new(
        x.Id,
        x.Code,
        x.NameAr,
        x.NameEn,
        x.Type,
        x.Email,
        x.PhoneNumber,
        x.ContactPerson,
        x.IsActive,
        x.Address,
        x.DepartmentId,
        x.Department?.NameAr,
        x.Department?.NameEn);
}
