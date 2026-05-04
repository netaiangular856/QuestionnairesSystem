using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuestionnairesSystem.Api.Authorization;
using QuestionnairesSystem.Application.Features.Identity.DTOs;
using QuestionnairesSystem.Application.Features.Questionnaires.Surveys.DTOs;
using QuestionnairesSystem.Domain.Enums;
using QuestionnairesSystem.Persistence;
using QuestionnairesSystem.Shared.Api;

namespace QuestionnairesSystem.Api.Controllers;

/// <summary>Lightweight id/name (and optional email) lists for dropdowns and typeahead.</summary>
[ApiController]
[Route("api/lookups")]
[Authorize(Policy = FormLookupAuthorizationPolicies.FormLookups)]
public sealed class QuestionnaireLookupsController : ControllerBase
{
    private readonly QuestionnairesDbContext _db;

    public QuestionnaireLookupsController(QuestionnairesDbContext db) => _db = db;

    /// <summary>Active users for fields like <c>assignedToUserId</c>.</summary>
    [HttpGet("users")]
    public async Task<IActionResult> Users(
        [FromQuery] string? search,
        [FromQuery] int take = 200,
        [FromQuery] bool activeOnly = true,
        CancellationToken cancellationToken = default)
    {
        var traceId = HttpContext.TraceIdentifier;
        var cap = Math.Clamp(take, 1, 500);
        var query = _db.Users.AsNoTracking().AsQueryable();
        if (activeOnly)
            query = query.Where(u => u.IsActive);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(u =>
                u.UserName.Contains(term)
                || u.Email.Contains(term)
                || (u.NameAr != null && u.NameAr.Contains(term))
                || (u.NameEn != null && u.NameEn.Contains(term)));
        }

        var rows = await query
            .OrderBy(u => u.NameAr)
            .ThenBy(u => u.NameEn)
            .ThenBy(u => u.UserName)
            .Take(cap)
            .Select(u => new { u.Id, u.NameAr, u.NameEn, u.UserName, u.Email })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var list = rows.ConvertAll(r => new LookupItemDto
        {
            Id = r.Id,
            Name = PickDisplayName(r.NameAr, r.NameEn, r.UserName),
            Email = r.Email
        });

        return Ok(ApiResponse<IReadOnlyList<LookupItemDto>>.FromSuccess(list, traceId));
    }

    [HttpGet("surveys")]
    public async Task<IActionResult> Surveys([FromQuery] string? search, [FromQuery] int take = 200, CancellationToken cancellationToken = default)
    {
        var traceId = HttpContext.TraceIdentifier;
        var cap = Math.Clamp(take, 1, 500);
        var query = _db.Surveys.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(s =>
                (s.Code != null && s.Code.Contains(term))
                || s.TitleAr.Contains(term)
                || s.TitleEn.Contains(term));
        }

        var rows = await query
            .OrderBy(s => s.TitleAr)
            .ThenBy(s => s.TitleEn)
            .Take(cap)
            .Select(s => new { s.Id, s.TitleAr, s.TitleEn, s.Code })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var list = rows.ConvertAll(r => new LookupItemDto
        {
            Id = r.Id,
            Name = PickDisplayName(r.TitleAr, r.TitleEn, r.Code ?? r.Id.ToString()[..8]),
            Email = null
        });

        return Ok(ApiResponse<IReadOnlyList<LookupItemDto>>.FromSuccess(list, traceId));
    }

    [HttpGet("templates")]
    public async Task<IActionResult> Templates(
        [FromQuery] string? search,
        [FromQuery] int take = 200,
        [FromQuery] bool includeArchived = false,
        CancellationToken cancellationToken = default)
    {
        var traceId = HttpContext.TraceIdentifier;
        var cap = Math.Clamp(take, 1, 500);
        var query = _db.SurveyTemplates.AsNoTracking().AsQueryable();
        if (!includeArchived)
            query = query.Where(t => !t.IsArchived);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(t => t.NameAr.Contains(term) || t.NameEn.Contains(term));
        }

        var rows = await query
            .OrderBy(t => t.NameAr)
            .ThenBy(t => t.NameEn)
            .Take(cap)
            .Select(t => new { t.Id, t.NameAr, t.NameEn })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var list = rows.ConvertAll(r => new LookupItemDto
        {
            Id = r.Id,
            Name = PickDisplayName(r.NameAr, r.NameEn, string.Empty),
            Email = null
        });

        return Ok(ApiResponse<IReadOnlyList<LookupItemDto>>.FromSuccess(list, traceId));
    }

    /// <summary>بحث موحّد: مستخدمون نشطون، موظفون بلا حساب (بريد)، متعاملون ببريد — للجمهور «مستخدمون محددون».</summary>
    [HttpGet("survey-audience-subjects")]
    public async Task<IActionResult> SurveyAudienceSubjects(
        [FromQuery] string? search,
        [FromQuery] int take = 60,
        CancellationToken cancellationToken = default)
    {
        var traceId = HttpContext.TraceIdentifier;
        var cap = Math.Clamp(take, 1, 200);
        var perKind = Math.Max(1, (cap + 2) / 3);
        var term = string.IsNullOrWhiteSpace(search) ? null : search.Trim();

        var usersQ = _db.Users.AsNoTracking().Where(u => u.IsActive);
        if (term is not null)
        {
            usersQ = usersQ.Where(u =>
                u.UserName.Contains(term)
                || u.Email.Contains(term)
                || (u.NameAr != null && u.NameAr.Contains(term))
                || (u.NameEn != null && u.NameEn.Contains(term)));
        }

        var userRows2 = await usersQ
            .OrderBy(u => u.NameAr)
            .ThenBy(u => u.NameEn)
            .Take(perKind)
            .Select(u => new { u.Id, u.NameAr, u.NameEn, u.UserName, u.Email })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var users = userRows2.ConvertAll(u => new SurveyAudienceLookupItemDto
        {
            Kind = SurveyAudienceSubjectKind.User,
            EntityId = u.Id,
            Name = PickDisplayName(u.NameAr, u.NameEn, u.UserName),
            Email = u.Email,
            UserId = u.Id
        });

        var empBase = _db.Employees.AsNoTracking()
            .Where(e => e.IsActive && e.Email != null && e.Email != "")
            .Where(e => !_db.Users.Any(u => u.EmployeeId == e.Id && u.IsActive));
        if (term is not null)
        {
            empBase = empBase.Where(e =>
                e.NameAr.Contains(term)
                || e.NameEn.Contains(term)
                || e.EmployeeNumber.Contains(term)
                || (e.Email != null && e.Email.Contains(term)));
        }

        var empRows = await empBase
            .OrderBy(e => e.NameAr)
            .ThenBy(e => e.NameEn)
            .Take(perKind)
            .Select(e => new { e.Id, e.NameAr, e.NameEn, e.EmployeeNumber, e.Email })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var employees = empRows.ConvertAll(e => new SurveyAudienceLookupItemDto
        {
            Kind = SurveyAudienceSubjectKind.Employee,
            EntityId = e.Id,
            Name = PickDisplayName(e.NameAr, e.NameEn, e.EmployeeNumber),
            Email = e.Email,
            UserId = null
        });

        var partQ = _db.Partners.AsNoTracking().Where(p => p.IsActive && p.Email != null && p.Email != "");
        if (term is not null)
        {
            partQ = partQ.Where(p =>
                p.NameAr.Contains(term)
                || p.NameEn.Contains(term)
                || p.Code.Contains(term)
                || (p.Email != null && p.Email.Contains(term)));
        }

        var partRows = await partQ
            .OrderBy(p => p.NameAr)
            .ThenBy(p => p.NameEn)
            .Take(perKind)
            .Select(p => new { p.Id, p.NameAr, p.NameEn, p.Code, p.Email })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var partners = partRows.ConvertAll(p => new SurveyAudienceLookupItemDto
        {
            Kind = SurveyAudienceSubjectKind.Partner,
            EntityId = p.Id,
            Name = PickDisplayName(p.NameAr, p.NameEn, p.Code),
            Email = p.Email,
            UserId = null
        });

        var merged = users
            .Concat(employees)
            .Concat(partners)
            .OrderBy(x => x.Name, StringComparer.OrdinalIgnoreCase)
            .Take(cap)
            .ToList();

        return Ok(ApiResponse<IReadOnlyList<SurveyAudienceLookupItemDto>>.FromSuccess(merged, traceId));
    }

    /// <summary>Generic lookup rows filtered by <paramref name="category"/> (domain table LookupItems).</summary>
    [HttpGet("items")]
    public async Task<IActionResult> LookupItemsByCategory(
        [FromQuery] string category,
        [FromQuery] string? search,
        [FromQuery] int take = 200,
        [FromQuery] bool activeOnly = true,
        CancellationToken cancellationToken = default)
    {
        var traceId = HttpContext.TraceIdentifier;
        if (string.IsNullOrWhiteSpace(category))
            return BadRequest(ApiResponse.FromFailure(new[] { "category is required." }, traceId));

        var cap = Math.Clamp(take, 1, 500);
        var cat = category.Trim();
        var query = _db.LookupItems.AsNoTracking().Where(x => x.Category == cat);
        if (activeOnly)
            query = query.Where(x => x.IsActive);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(x =>
                x.Code.Contains(term) || x.NameAr.Contains(term) || x.NameEn.Contains(term));
        }

        var rows = await query
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.NameAr)
            .Take(cap)
            .Select(x => new { x.Id, x.NameAr, x.NameEn })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var list = rows.ConvertAll(r => new LookupItemDto
        {
            Id = r.Id,
            Name = PickDisplayName(r.NameAr, r.NameEn, string.Empty),
            Email = null
        });

        return Ok(ApiResponse<IReadOnlyList<LookupItemDto>>.FromSuccess(list, traceId));
    }

    private static string PickDisplayName(string? ar, string? en, string fallback)
    {
        if (!string.IsNullOrWhiteSpace(ar)) return ar.Trim();
        if (!string.IsNullOrWhiteSpace(en)) return en.Trim();
        return string.IsNullOrWhiteSpace(fallback) ? "—" : fallback.Trim();
    }
}
