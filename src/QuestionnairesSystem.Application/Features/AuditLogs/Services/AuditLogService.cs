using Microsoft.EntityFrameworkCore;
using QuestionnairesSystem.Application.Features.AuditLogs.DTOs;
using QuestionnairesSystem.Application.Features.AuditLogs.Interfaces;
using QuestionnairesSystem.Persistence;
using QuestionnairesSystem.Shared.Api;
using QuestionnairesSystem.Shared.Constants;
using QuestionnairesSystem.Shared.Results;

namespace QuestionnairesSystem.Application.Features.AuditLogs.Services;

public sealed class AuditLogService : IAuditLogService
{
    private readonly QuestionnairesDbContext _db;

    public AuditLogService(QuestionnairesDbContext db)
    {
        _db = db;
    }

    public async Task<Result<PagedResult<AuditLogDto>>> GetPagedAsync(
        AuditLogFilterRequest request,
        CancellationToken cancellationToken = default)
    {
        var page = request.Page <= 0 ? PaginationConstants.DefaultPage : request.Page;
        var pageSize = request.PageSize <= 0 ? PaginationConstants.DefaultPageSize : request.PageSize;
        if (pageSize > PaginationConstants.MaxPageSize)
        {
            pageSize = PaginationConstants.MaxPageSize;
        }

        var query = _db.AuditLogs.AsNoTracking().AsQueryable();

        if (request.UserId.HasValue)
        {
            query = query.Where(x => x.UserId == request.UserId.Value);
        }

        if (!string.IsNullOrWhiteSpace(request.Action))
        {
            var action = request.Action.Trim();
            query = query.Where(x => x.Action.Contains(action));
        }

        if (!string.IsNullOrWhiteSpace(request.EntityType))
        {
            var entityType = request.EntityType.Trim();
            query = query.Where(x => x.EntityType.Contains(entityType));
        }

        if (request.FromUtc.HasValue)
        {
            query = query.Where(x => x.OccurredAtUtc >= request.FromUtc.Value);
        }

        if (request.ToUtc.HasValue)
        {
            query = query.Where(x => x.OccurredAtUtc <= request.ToUtc.Value);
        }

        var total = await query.CountAsync(cancellationToken).ConfigureAwait(false);
        var items = await query
            .OrderByDescending(x => x.OccurredAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new AuditLogDto
            {
                Id = x.Id,
                OccurredAtUtc = x.OccurredAtUtc,
                UserId = x.UserId,
                UserName = x.UserId == null
                    ? null
                    : _db.Users
                        .Where(u => u.Id == x.UserId.Value)
                        .Select(u => string.IsNullOrWhiteSpace(u.NameAr) ? u.UserName : u.NameAr)
                        .FirstOrDefault(),
                Action = x.Action,
                EntityType = x.EntityType,
                EntityId = x.EntityId,
                OldValuesJson = x.OldValuesJson,
                NewValuesJson = x.NewValuesJson,
                IpAddress = x.IpAddress,
                UserAgent = x.UserAgent
            })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return Result<PagedResult<AuditLogDto>>.Ok(new PagedResult<AuditLogDto>
        {
            Items = items,
            TotalCount = total,
            Page = page,
            PageSize = pageSize
        });
    }

    public async Task<Result<AuditLogDto>> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var log = await _db.AuditLogs.AsNoTracking()
            .Where(x => x.Id == id)
            .Select(x => new AuditLogDto
            {
                Id = x.Id,
                OccurredAtUtc = x.OccurredAtUtc,
                UserId = x.UserId,
                UserName = x.UserId == null
                    ? null
                    : _db.Users
                        .Where(u => u.Id == x.UserId.Value)
                        .Select(u => string.IsNullOrWhiteSpace(u.NameAr) ? u.UserName : u.NameAr)
                        .FirstOrDefault(),
                Action = x.Action,
                EntityType = x.EntityType,
                EntityId = x.EntityId,
                OldValuesJson = x.OldValuesJson,
                NewValuesJson = x.NewValuesJson,
                IpAddress = x.IpAddress,
                UserAgent = x.UserAgent
            })
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);

        return log is null
            ? Result<AuditLogDto>.Fail("Audit log was not found.")
            : Result<AuditLogDto>.Ok(log);
    }
}
