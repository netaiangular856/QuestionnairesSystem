using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuestionnairesSystem.Application.Features.Identity;
using QuestionnairesSystem.Application.Features.Identity.DTOs;
using QuestionnairesSystem.Persistence;
using QuestionnairesSystem.Shared.Api;

namespace QuestionnairesSystem.Api.Controllers;

[ApiController]
[Route("api/identity-lookups")]
[Authorize(Policy = PermissionCodes.UserManage)]
public sealed class IdentityLookupsController : ControllerBase
{
    private readonly QuestionnairesDbContext _db;

    public IdentityLookupsController(QuestionnairesDbContext db)
    {
        _db = db;
    }

    [HttpGet("roles")]
    public async Task<IActionResult> GetRoles([FromQuery] string? search, [FromQuery] int take = 200, CancellationToken cancellationToken = default)
    {
        var traceId = HttpContext.TraceIdentifier;
        var cap = Math.Clamp(take, 1, 500);
        var query = _db.Roles.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(r => r.NameAr.Contains(term) || r.NameEn.Contains(term));
        }

        var rows = await query
            .OrderBy(r => r.NameAr)
            .ThenBy(r => r.NameEn)
            .Take(cap)
            .Select(r => new LookupItemDto
            {
                Id = r.Id,
                Name = string.IsNullOrWhiteSpace(r.NameAr) ? r.NameEn : r.NameAr
            })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return Ok(ApiResponse<IReadOnlyList<LookupItemDto>>.FromSuccess(rows, traceId));
    }
}
