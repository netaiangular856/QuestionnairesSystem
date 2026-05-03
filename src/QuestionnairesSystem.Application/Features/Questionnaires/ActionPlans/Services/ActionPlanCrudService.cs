using FluentValidation;
using Microsoft.EntityFrameworkCore;
using QuestionnairesSystem.Application.Common;
using QuestionnairesSystem.Application.Features.Questionnaires.ActionPlans.DTOs;
using QuestionnairesSystem.Application.Features.Questionnaires.ActionPlans.Interfaces;
using QuestionnairesSystem.Domain.ActionPlans;
using QuestionnairesSystem.Domain.Enums;
using QuestionnairesSystem.Persistence;
using QuestionnairesSystem.Shared.Api;
using QuestionnairesSystem.Shared.Results;

namespace QuestionnairesSystem.Application.Features.Questionnaires.ActionPlans.Services;

public sealed class ActionPlanCrudService : IActionPlanCrudService
{
    private readonly QuestionnairesDbContext _db;
    private readonly IValidator<CreateActionPlanRequest> _createPlanValidator;
    private readonly IValidator<UpdateActionPlanRequest> _updatePlanValidator;
    private readonly IValidator<CreateInitiativeRequest> _createInitiativeValidator;
    private readonly IValidator<UpdateInitiativeRequest> _updateInitiativeValidator;
    private readonly IValidator<AddInitiativeProgressRequest> _progressValidator;

    public ActionPlanCrudService(
        QuestionnairesDbContext db,
        IValidator<CreateActionPlanRequest> createPlanValidator,
        IValidator<UpdateActionPlanRequest> updatePlanValidator,
        IValidator<CreateInitiativeRequest> createInitiativeValidator,
        IValidator<UpdateInitiativeRequest> updateInitiativeValidator,
        IValidator<AddInitiativeProgressRequest> progressValidator)
    {
        _db = db;
        _createPlanValidator = createPlanValidator;
        _updatePlanValidator = updatePlanValidator;
        _createInitiativeValidator = createInitiativeValidator;
        _updateInitiativeValidator = updateInitiativeValidator;
        _progressValidator = progressValidator;
    }

    public async Task<Result<ActionPlanDto>> CreateAsync(CreateActionPlanRequest request, CancellationToken cancellationToken = default)
    {
        var validation = await _createPlanValidator.ValidateAsync(request, cancellationToken).ConfigureAwait(false);
        if (!validation.IsValid)
            return Result<ActionPlanDto>.Fail(validation.ToErrorMessages());

        if (request.SurveyId is { } sid && !await _db.Surveys.AnyAsync(s => s.Id == sid, cancellationToken).ConfigureAwait(false))
            return Result<ActionPlanDto>.Fail("Survey was not found.", QuestionnaireErrors.SurveyNotFound);

        var p = new ActionPlan
        {
            TitleAr = request.TitleAr.Trim(),
            TitleEn = request.TitleEn.Trim(),
            DescriptionAr = string.IsNullOrWhiteSpace(request.DescriptionAr) ? null : request.DescriptionAr.Trim(),
            DescriptionEn = string.IsNullOrWhiteSpace(request.DescriptionEn) ? null : request.DescriptionEn.Trim(),
            SurveyId = request.SurveyId,
            OwnerUserId = request.OwnerUserId,
            Status = ActionPlanStatus.Draft,
            StartDateUtc = request.StartDateUtc,
            EndDateUtc = request.EndDateUtc
        };
        _db.ActionPlans.Add(p);
        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        var created = await SelectActionPlanDto(_db.ActionPlans.AsNoTracking().Where(x => x.Id == p.Id))
            .FirstAsync(cancellationToken)
            .ConfigureAwait(false);
        return Result<ActionPlanDto>.Ok(created);
    }

    public async Task<Result<IReadOnlyList<ActionPlanDto>>> ListAsync(CancellationToken cancellationToken = default)
    {
        var list = await SelectActionPlanDto(
                _db.ActionPlans.AsNoTracking().OrderByDescending(x => x.CreatedOnUtc))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        return Result<IReadOnlyList<ActionPlanDto>>.Ok(list);
    }

    public async Task<Result<PagedResult<ActionPlanDto>>> ListPagedAsync(int page, int pageSize, CancellationToken cancellationToken = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);
        var baseQuery = _db.ActionPlans.AsNoTracking();
        var totalCount = await baseQuery.CountAsync(cancellationToken).ConfigureAwait(false);
        var items = await SelectActionPlanDto(
                baseQuery
                    .OrderByDescending(x => x.CreatedOnUtc)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        var paged = new PagedResult<ActionPlanDto>
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount,
        };
        return Result<PagedResult<ActionPlanDto>>.Ok(paged);
    }

    public async Task<Result<ActionPlanDto>> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var dto = await SelectActionPlanDto(_db.ActionPlans.AsNoTracking().Where(x => x.Id == id))
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);
        return dto is null
            ? Result<ActionPlanDto>.Fail("Action plan was not found.", QuestionnaireErrors.ActionPlanNotFound)
            : Result<ActionPlanDto>.Ok(dto);
    }

    public async Task<Result<ActionPlanDto>> UpdateAsync(
        Guid id,
        UpdateActionPlanRequest request,
        CancellationToken cancellationToken = default)
    {
        var validation = await _updatePlanValidator.ValidateAsync(request, cancellationToken).ConfigureAwait(false);
        if (!validation.IsValid)
            return Result<ActionPlanDto>.Fail(validation.ToErrorMessages());

        var p = await _db.ActionPlans.FirstOrDefaultAsync(x => x.Id == id, cancellationToken).ConfigureAwait(false);
        if (p is null)
            return Result<ActionPlanDto>.Fail("Action plan was not found.", QuestionnaireErrors.ActionPlanNotFound);

        if (request.SurveyId is { } sid && !await _db.Surveys.AnyAsync(s => s.Id == sid, cancellationToken).ConfigureAwait(false))
            return Result<ActionPlanDto>.Fail("Survey was not found.", QuestionnaireErrors.SurveyNotFound);

        p.TitleAr = request.TitleAr.Trim();
        p.TitleEn = request.TitleEn.Trim();
        p.DescriptionAr = string.IsNullOrWhiteSpace(request.DescriptionAr) ? null : request.DescriptionAr.Trim();
        p.DescriptionEn = string.IsNullOrWhiteSpace(request.DescriptionEn) ? null : request.DescriptionEn.Trim();
        p.SurveyId = request.SurveyId;
        p.OwnerUserId = request.OwnerUserId;
        p.Status = request.Status;
        p.StartDateUtc = request.StartDateUtc;
        p.EndDateUtc = request.EndDateUtc;
        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        var updatedPlan = await SelectActionPlanDto(_db.ActionPlans.AsNoTracking().Where(x => x.Id == p.Id))
            .FirstAsync(cancellationToken)
            .ConfigureAwait(false);
        return Result<ActionPlanDto>.Ok(updatedPlan);
    }

    public async Task<Result<IReadOnlyList<InitiativeDto>>> ListInitiativesByActionPlanAsync(
        Guid actionPlanId,
        CancellationToken cancellationToken = default)
    {
        var exists = await _db.ActionPlans.AnyAsync(x => x.Id == actionPlanId, cancellationToken).ConfigureAwait(false);
        if (!exists)
        {
            return Result<IReadOnlyList<InitiativeDto>>.Fail(
                "Action plan was not found.",
                QuestionnaireErrors.ActionPlanNotFound);
        }

        var list = await SelectInitiativeDto(
                _db.Initiatives.AsNoTracking()
                    .Where(x => x.ActionPlanId == actionPlanId)
                    .OrderBy(x => x.TitleEn))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        return Result<IReadOnlyList<InitiativeDto>>.Ok(list);
    }

    public async Task<Result<InitiativeDto>> AddInitiativeAsync(
        Guid actionPlanId,
        CreateInitiativeRequest request,
        CancellationToken cancellationToken = default)
    {
        var validation = await _createInitiativeValidator.ValidateAsync(request, cancellationToken).ConfigureAwait(false);
        if (!validation.IsValid)
            return Result<InitiativeDto>.Fail(validation.ToErrorMessages());

        var plan = await _db.ActionPlans.AnyAsync(x => x.Id == actionPlanId, cancellationToken).ConfigureAwait(false);
        if (!plan)
            return Result<InitiativeDto>.Fail("Action plan was not found.", QuestionnaireErrors.ActionPlanNotFound);

        var i = new Initiative
        {
            ActionPlanId = actionPlanId,
            TitleAr = request.TitleAr.Trim(),
            TitleEn = request.TitleEn.Trim(),
            DescriptionAr = string.IsNullOrWhiteSpace(request.DescriptionAr) ? null : request.DescriptionAr.Trim(),
            DescriptionEn = string.IsNullOrWhiteSpace(request.DescriptionEn) ? null : request.DescriptionEn.Trim(),
            Status = InitiativeStatus.Planned,
            OwnerUserId = request.OwnerUserId,
            TargetDateUtc = request.TargetDateUtc
        };
        _db.Initiatives.Add(i);
        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        var createdInit = await SelectInitiativeDto(_db.Initiatives.AsNoTracking().Where(x => x.Id == i.Id))
            .FirstAsync(cancellationToken)
            .ConfigureAwait(false);
        return Result<InitiativeDto>.Ok(createdInit);
    }

    public async Task<Result<IReadOnlyList<InitiativeListItemDto>>> ListAllInitiativesAsync(
        CancellationToken cancellationToken = default)
    {
        var list = await _db.Initiatives.AsNoTracking()
            .OrderByDescending(i => i.CreatedOnUtc)
            .Select(i => new InitiativeListItemDto
            {
                Id = i.Id,
                ActionPlanId = i.ActionPlanId,
                TitleAr = i.TitleAr,
                TitleEn = i.TitleEn,
                Status = i.Status,
                OwnerDisplayName = i.OwnerUser == null
                    ? null
                    : (i.OwnerUser.NameAr ?? i.OwnerUser.NameEn ?? i.OwnerUser.UserName),
                TargetDateUtc = i.TargetDateUtc,
                ActionPlanTitleAr = i.ActionPlan.TitleAr,
                ActionPlanTitleEn = i.ActionPlan.TitleEn,
            })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        return Result<IReadOnlyList<InitiativeListItemDto>>.Ok(list);
    }

    public async Task<Result<PagedResult<InitiativeListItemDto>>> ListInitiativesPagedAsync(
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);
        var baseQuery = _db.Initiatives.AsNoTracking();
        var totalCount = await baseQuery.CountAsync(cancellationToken).ConfigureAwait(false);
        var items = await baseQuery
            .OrderByDescending(i => i.CreatedOnUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(i => new InitiativeListItemDto
            {
                Id = i.Id,
                ActionPlanId = i.ActionPlanId,
                TitleAr = i.TitleAr,
                TitleEn = i.TitleEn,
                Status = i.Status,
                OwnerDisplayName = i.OwnerUser == null
                    ? null
                    : (i.OwnerUser.NameAr ?? i.OwnerUser.NameEn ?? i.OwnerUser.UserName),
                TargetDateUtc = i.TargetDateUtc,
                ActionPlanTitleAr = i.ActionPlan.TitleAr,
                ActionPlanTitleEn = i.ActionPlan.TitleEn,
            })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        var paged = new PagedResult<InitiativeListItemDto>
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount,
        };
        return Result<PagedResult<InitiativeListItemDto>>.Ok(paged);
    }

    public async Task<Result<InitiativeDto>> GetInitiativeAsync(Guid initiativeId, CancellationToken cancellationToken = default)
    {
        var dto = await SelectInitiativeDto(_db.Initiatives.AsNoTracking().Where(x => x.Id == initiativeId))
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);
        return dto is null
            ? Result<InitiativeDto>.Fail("Initiative was not found.", QuestionnaireErrors.InitiativeNotFound)
            : Result<InitiativeDto>.Ok(dto);
    }

    public async Task<Result<InitiativeDto>> UpdateInitiativeAsync(
        Guid initiativeId,
        UpdateInitiativeRequest request,
        CancellationToken cancellationToken = default)
    {
        var validation = await _updateInitiativeValidator.ValidateAsync(request, cancellationToken).ConfigureAwait(false);
        if (!validation.IsValid)
            return Result<InitiativeDto>.Fail(validation.ToErrorMessages());

        var i = await _db.Initiatives.FirstOrDefaultAsync(x => x.Id == initiativeId, cancellationToken).ConfigureAwait(false);
        if (i is null)
            return Result<InitiativeDto>.Fail("Initiative was not found.", QuestionnaireErrors.InitiativeNotFound);

        i.TitleAr = request.TitleAr.Trim();
        i.TitleEn = request.TitleEn.Trim();
        i.DescriptionAr = string.IsNullOrWhiteSpace(request.DescriptionAr) ? null : request.DescriptionAr.Trim();
        i.DescriptionEn = string.IsNullOrWhiteSpace(request.DescriptionEn) ? null : request.DescriptionEn.Trim();
        i.Status = request.Status;
        i.OwnerUserId = request.OwnerUserId;
        i.TargetDateUtc = request.TargetDateUtc;
        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        var updatedInit = await SelectInitiativeDto(_db.Initiatives.AsNoTracking().Where(x => x.Id == i.Id))
            .FirstAsync(cancellationToken)
            .ConfigureAwait(false);
        return Result<InitiativeDto>.Ok(updatedInit);
    }

    public async Task<Result<InitiativeProgressDto>> AddProgressAsync(
        Guid initiativeId,
        AddInitiativeProgressRequest request,
        CancellationToken cancellationToken = default)
    {
        var validation = await _progressValidator.ValidateAsync(request, cancellationToken).ConfigureAwait(false);
        if (!validation.IsValid)
            return Result<InitiativeProgressDto>.Fail(validation.ToErrorMessages());

        var exists = await _db.Initiatives.AnyAsync(x => x.Id == initiativeId, cancellationToken).ConfigureAwait(false);
        if (!exists)
            return Result<InitiativeProgressDto>.Fail("Initiative was not found.", QuestionnaireErrors.InitiativeNotFound);

        var e = new InitiativeProgress
        {
            InitiativeId = initiativeId,
            ProgressPercent = request.ProgressPercent,
            Notes = string.IsNullOrWhiteSpace(request.Notes) ? null : request.Notes.Trim(),
            RecordedAtUtc = DateTime.UtcNow
        };
        _db.InitiativeProgressEntries.Add(e);
        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        var progressDto = await SelectInitiativeProgressDto(
                _db.InitiativeProgressEntries.AsNoTracking().Where(x => x.Id == e.Id))
            .FirstAsync(cancellationToken)
            .ConfigureAwait(false);
        return Result<InitiativeProgressDto>.Ok(progressDto);
    }

    public async Task<Result<IReadOnlyList<InitiativeProgressDto>>> ListProgressAsync(
        Guid initiativeId,
        CancellationToken cancellationToken = default)
    {
        var exists = await _db.Initiatives.AnyAsync(x => x.Id == initiativeId, cancellationToken).ConfigureAwait(false);
        if (!exists)
        {
            return Result<IReadOnlyList<InitiativeProgressDto>>.Fail(
                "Initiative was not found.",
                QuestionnaireErrors.InitiativeNotFound);
        }

        var list = await SelectInitiativeProgressDto(
                _db.InitiativeProgressEntries.AsNoTracking()
                    .Where(x => x.InitiativeId == initiativeId)
                    .OrderByDescending(x => x.RecordedAtUtc))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        return Result<IReadOnlyList<InitiativeProgressDto>>.Ok(list);
    }

    private static IQueryable<ActionPlanDto> SelectActionPlanDto(IQueryable<ActionPlan> query) =>
        query.Select(p => new ActionPlanDto
        {
            Id = p.Id,
            TitleAr = p.TitleAr,
            TitleEn = p.TitleEn,
            DescriptionAr = p.DescriptionAr,
            DescriptionEn = p.DescriptionEn,
            SurveyId = p.SurveyId,
            OwnerUserId = p.OwnerUserId,
            OwnerDisplayName = p.OwnerUser == null
                ? null
                : (p.OwnerUser.NameAr ?? p.OwnerUser.NameEn ?? p.OwnerUser.UserName),
            Status = p.Status,
            StartDateUtc = p.StartDateUtc,
            EndDateUtc = p.EndDateUtc
        });

    private static IQueryable<InitiativeDto> SelectInitiativeDto(IQueryable<Initiative> query) =>
        query.Select(i => new InitiativeDto
        {
            Id = i.Id,
            ActionPlanId = i.ActionPlanId,
            TitleAr = i.TitleAr,
            TitleEn = i.TitleEn,
            DescriptionAr = i.DescriptionAr,
            DescriptionEn = i.DescriptionEn,
            Status = i.Status,
            OwnerUserId = i.OwnerUserId,
            OwnerDisplayName = i.OwnerUser == null
                ? null
                : (i.OwnerUser.NameAr ?? i.OwnerUser.NameEn ?? i.OwnerUser.UserName),
            TargetDateUtc = i.TargetDateUtc
        });

    private static IQueryable<InitiativeProgressDto> SelectInitiativeProgressDto(IQueryable<InitiativeProgress> query) =>
        query.Select(e => new InitiativeProgressDto
        {
            Id = e.Id,
            InitiativeId = e.InitiativeId,
            ProgressPercent = e.ProgressPercent,
            Notes = e.Notes,
            RecordedAtUtc = e.RecordedAtUtc,
            RecordedByDisplayName = e.RecordedByUser == null
                ? null
                : (e.RecordedByUser.NameAr ?? e.RecordedByUser.NameEn ?? e.RecordedByUser.UserName)
        });
}
