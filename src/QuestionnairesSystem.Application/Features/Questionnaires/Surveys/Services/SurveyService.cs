using System.Text.Json;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using QuestionnairesSystem.Application.Common;
using QuestionnairesSystem.Application.Features.Questionnaires.Surveys.DTOs;
using QuestionnairesSystem.Application.Features.Questionnaires.Surveys.Interfaces;
using QuestionnairesSystem.Domain.Enums;
using QuestionnairesSystem.Domain.Surveys;
using QuestionnairesSystem.Domain.Templates;
using QuestionnairesSystem.Persistence;
using QuestionnairesSystem.Shared.Api;
using QuestionnairesSystem.Shared.Constants;
using QuestionnairesSystem.Shared.Identity;
using QuestionnairesSystem.Shared.Results;

namespace QuestionnairesSystem.Application.Features.Questionnaires.Surveys.Services;

public sealed class SurveyService : ISurveyService
{
    private readonly QuestionnairesDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly IValidator<CreateSurveyRequest> _createValidator;
    private readonly IValidator<UpdateSurveyRequest> _updateValidator;
    private readonly IValidator<SurveyFilterRequest> _filterValidator;
    private readonly IValidator<RejectSurveyRequest> _rejectValidator;
    private readonly IValidator<PatchSurveyStatusRequest> _patchStatusValidator;
    private readonly IValidator<PublishSurveyRequest> _publishValidator;

    public SurveyService(
        QuestionnairesDbContext db,
        ICurrentUserService currentUser,
        IValidator<CreateSurveyRequest> createValidator,
        IValidator<UpdateSurveyRequest> updateValidator,
        IValidator<SurveyFilterRequest> filterValidator,
        IValidator<RejectSurveyRequest> rejectValidator,
        IValidator<PatchSurveyStatusRequest> patchStatusValidator,
        IValidator<PublishSurveyRequest> publishValidator)
    {
        _db = db;
        _currentUser = currentUser;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
        _filterValidator = filterValidator;
        _rejectValidator = rejectValidator;
        _patchStatusValidator = patchStatusValidator;
        _publishValidator = publishValidator;
    }

    public async Task<Result<SurveyDetailDto>> CreateAsync(CreateSurveyRequest request, CancellationToken cancellationToken = default)
    {
        var validation = await _createValidator.ValidateAsync(request, cancellationToken).ConfigureAwait(false);
        if (!validation.IsValid)
            return Result<SurveyDetailDto>.Fail(validation.ToErrorMessages());

        var survey = new Survey
        {
            TitleAr = request.TitleAr.Trim(),
            TitleEn = request.TitleEn.Trim(),
            DescriptionAr = string.IsNullOrWhiteSpace(request.DescriptionAr) ? null : request.DescriptionAr.Trim(),
            DescriptionEn = string.IsNullOrWhiteSpace(request.DescriptionEn) ? null : request.DescriptionEn.Trim(),
            Code = string.IsNullOrWhiteSpace(request.Code) ? null : request.Code.Trim(),
            AudienceScope = request.AudienceScope,
            Status = SurveyStatus.Draft,
            OwnerUserId = _currentUser.UserId,
            TemplateId = request.TemplateId,
            OpensAtUtc = request.OpensAtUtc,
            ClosesAtUtc = request.ClosesAtUtc
        };

        if (request.TemplateId is { } tid)
        {
            var tpl = await _db.SurveyTemplates.FirstOrDefaultAsync(t => t.Id == tid, cancellationToken).ConfigureAwait(false);
            if (tpl is null)
                return Result<SurveyDetailDto>.Fail("Template was not found.", QuestionnaireErrors.TemplateNotFound);

            TryApplyTemplateStructure(survey, tpl.StructureJson);
            tpl.UsageCount++;
        }

        AppendOwnerQuestions(survey, request.Questions);

        _db.Surveys.Add(survey);
        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Result<SurveyDetailDto>.Ok(await MapDetailAsync(survey, cancellationToken).ConfigureAwait(false));
    }

    public async Task<Result<PagedResult<SurveyListItemDto>>> GetPagedAsync(
        SurveyFilterRequest request,
        CancellationToken cancellationToken = default)
    {
        var validation = await _filterValidator.ValidateAsync(request, cancellationToken).ConfigureAwait(false);
        if (!validation.IsValid)
            return Result<PagedResult<SurveyListItemDto>>.Fail(validation.ToErrorMessages());

        var page = request.Page <= 0 ? PaginationConstants.DefaultPage : request.Page;
        var pageSize = request.PageSize <= 0 ? PaginationConstants.DefaultPageSize : request.PageSize;
        if (pageSize > PaginationConstants.MaxPageSize) pageSize = PaginationConstants.MaxPageSize;

        var q = _db.Surveys.AsNoTracking();
        if (request.Status is { } st)
            q = q.Where(s => s.Status == st);

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var term = request.Search.Trim();
            q = q.Where(s =>
                s.TitleAr.Contains(term) ||
                s.TitleEn.Contains(term) ||
                (s.Code != null && s.Code.Contains(term)));
        }

        var total = await q.CountAsync(cancellationToken).ConfigureAwait(false);
        var ids = await q
            .OrderByDescending(s => s.CreatedOnUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(s => s.Id)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var items = new List<SurveyListItemDto>();
        foreach (var id in ids)
        {
            var row = await _db.Surveys.AsNoTracking()
                .Where(s => s.Id == id)
                .Select(s => new
                {
                    s.Id,
                    s.TitleAr,
                    s.TitleEn,
                    s.Code,
                    s.Status,
                    s.Version,
                    s.PublishedAtUtc,
                    s.OpensAtUtc,
                    s.ClosesAtUtc,
                    OwnerDisplayName = s.Owner == null
                        ? null
                        : (s.Owner.NameAr ?? s.Owner.NameEn ?? s.Owner.UserName),
                    QuestionCount = s.Questions.Count,
                    ResponseCount = s.Responses.Count(r => r.Status == ResponseStatus.Submitted)
                })
                .FirstAsync(cancellationToken)
                .ConfigureAwait(false);
            items.Add(new SurveyListItemDto
            {
                Id = row.Id,
                TitleAr = row.TitleAr,
                TitleEn = row.TitleEn,
                Code = row.Code,
                Status = row.Status,
                Version = row.Version,
                OwnerDisplayName = row.OwnerDisplayName,
                PublishedAtUtc = row.PublishedAtUtc,
                OpensAtUtc = row.OpensAtUtc,
                ClosesAtUtc = row.ClosesAtUtc,
                QuestionCount = row.QuestionCount,
                ResponseCount = row.ResponseCount
            });
        }

        return Result<PagedResult<SurveyListItemDto>>.Ok(new PagedResult<SurveyListItemDto>
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalCount = total
        });
    }

    public async Task<Result<SurveyDetailDto>> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var s = await _db.Surveys.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, cancellationToken).ConfigureAwait(false);
        return s is null
            ? Result<SurveyDetailDto>.Fail("Survey was not found.", QuestionnaireErrors.SurveyNotFound)
            : Result<SurveyDetailDto>.Ok(await MapDetailAsync(s, cancellationToken).ConfigureAwait(false));
    }

    public async Task<Result<SurveyDetailDto>> UpdateAsync(Guid id, UpdateSurveyRequest request, CancellationToken cancellationToken = default)
    {
        var validation = await _updateValidator.ValidateAsync(request, cancellationToken).ConfigureAwait(false);
        if (!validation.IsValid)
            return Result<SurveyDetailDto>.Fail(validation.ToErrorMessages());

        var s = await _db.Surveys
            .Include(x => x.Questions)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken).ConfigureAwait(false);
        if (s is null)
            return Result<SurveyDetailDto>.Fail("Survey was not found.", QuestionnaireErrors.SurveyNotFound);

        s.TitleAr = request.TitleAr.Trim();
        s.TitleEn = request.TitleEn.Trim();
        s.DescriptionAr = string.IsNullOrWhiteSpace(request.DescriptionAr) ? null : request.DescriptionAr.Trim();
        s.DescriptionEn = string.IsNullOrWhiteSpace(request.DescriptionEn) ? null : request.DescriptionEn.Trim();
        s.Code = string.IsNullOrWhiteSpace(request.Code) ? null : request.Code.Trim();
        s.AudienceScope = request.AudienceScope;

        if (request.Questions is not null)
        {
            // Only allow updating questions if Draft or Rejected
            if (s.Status != SurveyStatus.Draft && s.Status != SurveyStatus.Rejected)
                return Result<SurveyDetailDto>.Fail("Questions can only be updated in Draft or Rejected status.");

            _db.Questions.RemoveRange(s.Questions);
            s.Questions.Clear();
            AppendOwnerQuestions(s, request.Questions);
        }

        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Result<SurveyDetailDto>.Ok(await MapDetailAsync(s, cancellationToken).ConfigureAwait(false));
    }

    public async Task<Result> SoftDeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var s = await _db.Surveys
            .Include(x => x.Questions)
            .Include(x => x.Participants)
            .Include(x => x.Responses)
            .ThenInclude(r => r.Answers)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
            .ConfigureAwait(false);
        if (s is null)
            return Result.Fail("Survey was not found.", QuestionnaireErrors.SurveyNotFound);

        SoftDeleteEntity(s);
        foreach (var q in s.Questions) SoftDeleteEntity(q);
        foreach (var p in s.Participants) SoftDeleteEntity(p);
        foreach (var r in s.Responses)
        {
            foreach (var a in r.Answers) SoftDeleteEntity(a);
            SoftDeleteEntity(r);
        }

        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Result.Ok();
    }

    public async Task<Result<SurveyDetailDto>> DuplicateAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var source = await _db.Surveys
            .Include(x => x.Questions)
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
            .ConfigureAwait(false);
        if (source is null)
            return Result<SurveyDetailDto>.Fail("Survey was not found.", QuestionnaireErrors.SurveyNotFound);

        var copy = new Survey
        {
            TitleAr = source.TitleAr + " — نسخة",
            TitleEn = source.TitleEn + " (copy)",
            DescriptionAr = source.DescriptionAr,
            DescriptionEn = source.DescriptionEn,
            Code = null,
            Status = SurveyStatus.Draft,
            AudienceScope = source.AudienceScope,
            Version = 1,
            OwnerUserId = _currentUser.UserId,
            TemplateId = source.TemplateId,
            OpensAtUtc = source.OpensAtUtc,
            ClosesAtUtc = source.ClosesAtUtc
        };

        _db.Surveys.Add(copy);
        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        foreach (var q in source.Questions.OrderBy(x => x.DisplayOrder))
        {
            _db.Questions.Add(new Question
            {
                SurveyId = copy.Id,
                DisplayOrder = q.DisplayOrder,
                Type = q.Type,
                TitleAr = q.TitleAr,
                TitleEn = q.TitleEn,
                HelpTextAr = q.HelpTextAr,
                HelpTextEn = q.HelpTextEn,
                IsRequired = q.IsRequired,
                OptionsJson = q.OptionsJson
            });
        }

        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Result<SurveyDetailDto>.Ok(await MapDetailAsync(copy, cancellationToken).ConfigureAwait(false));
    }

    public async Task<Result<SurveyDetailDto>> PatchStatusAsync(
        Guid id,
        PatchSurveyStatusRequest request,
        CancellationToken cancellationToken = default)
    {
        var validation = await _patchStatusValidator.ValidateAsync(request, cancellationToken).ConfigureAwait(false);
        if (!validation.IsValid)
            return Result<SurveyDetailDto>.Fail(validation.ToErrorMessages());

        return await SetStatusAsync(id, request.Status, null, cancellationToken).ConfigureAwait(false);
    }

    public Task<Result<SurveyDetailDto>> SubmitForApprovalAsync(Guid id, CancellationToken cancellationToken = default) =>
        SetStatusAsync(id, SurveyStatus.PendingApproval, null, cancellationToken);

    public Task<Result<SurveyDetailDto>> ApproveAsync(Guid id, CancellationToken cancellationToken = default) =>
        SetStatusAsync(id, SurveyStatus.Approved, null, cancellationToken);

    public async Task<Result<SurveyDetailDto>> RejectAsync(Guid id, RejectSurveyRequest request, CancellationToken cancellationToken = default)
    {
        var validation = await _rejectValidator.ValidateAsync(request, cancellationToken).ConfigureAwait(false);
        if (!validation.IsValid)
            return Result<SurveyDetailDto>.Fail(validation.ToErrorMessages());

        return await SetStatusAsync(id, SurveyStatus.Rejected, request.Reason, cancellationToken).ConfigureAwait(false);
    }

    public async Task<Result<SurveyDetailDto>> PublishAsync(
        Guid id,
        PublishSurveyRequest? publishRequest,
        CancellationToken cancellationToken = default)
    {
        if (publishRequest is not null)
        {
            var pv = await _publishValidator.ValidateAsync(publishRequest, cancellationToken).ConfigureAwait(false);
            if (!pv.IsValid)
                return Result<SurveyDetailDto>.Fail(pv.ToErrorMessages());
        }

        await ApplyPublishAudienceAsync(id, publishRequest, cancellationToken).ConfigureAwait(false);

        var scope = await _db.Surveys.AsNoTracking()
            .Where(s => s.Id == id)
            .Select(s => s.AudienceScope)
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);

        if (scope == SurveyAudienceScope.SpecificUsers)
        {
            var memberCount = await _db.SurveyAudienceMembers
                .CountAsync(m => m.SurveyId == id, cancellationToken)
                .ConfigureAwait(false);
            if (memberCount == 0)
            {
                return Result<SurveyDetailDto>.Fail(
                    "SpecificUsers audience requires at least one audience member.",
                    QuestionnaireErrors.InvalidOperation);
            }
        }

        return await SetStatusAsync(id, SurveyStatus.Published, null, cancellationToken).ConfigureAwait(false);
    }

    public Task<Result<SurveyDetailDto>> CloseAsync(Guid id, CancellationToken cancellationToken = default) =>
        SetStatusAsync(id, SurveyStatus.Closed, null, cancellationToken);

    private async Task<Result<SurveyDetailDto>> SetStatusAsync(
        Guid id,
        SurveyStatus target,
        string? rejectionReason,
        CancellationToken cancellationToken)
    {
        var s = await _db.Surveys.FirstOrDefaultAsync(x => x.Id == id, cancellationToken).ConfigureAwait(false);
        if (s is null)
            return Result<SurveyDetailDto>.Fail("Survey was not found.", QuestionnaireErrors.SurveyNotFound);

        if (!IsTransitionAllowed(s.Status, target))
            return Result<SurveyDetailDto>.Fail("Status transition is not allowed.", QuestionnaireErrors.InvalidStatusTransition);

        s.Status = target;
        if (target == SurveyStatus.Rejected)
            s.RejectionReason = string.IsNullOrWhiteSpace(rejectionReason) ? "Rejected" : rejectionReason.Trim();
        else if (target != SurveyStatus.Rejected)
            s.RejectionReason = null;

        if (target == SurveyStatus.Published)
            s.PublishedAtUtc = DateTime.UtcNow;

        if (target == SurveyStatus.Closed)
            s.ClosedAtUtc = DateTime.UtcNow;

        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Result<SurveyDetailDto>.Ok(await MapDetailAsync(s, cancellationToken).ConfigureAwait(false));
    }

    private static bool IsTransitionAllowed(SurveyStatus current, SurveyStatus next) =>
        (current, next) switch
        {
            (SurveyStatus.Draft, SurveyStatus.PendingApproval) => true,
            (SurveyStatus.PendingApproval, SurveyStatus.Approved) => true,
            (SurveyStatus.PendingApproval, SurveyStatus.Rejected) => true,
            (SurveyStatus.Approved, SurveyStatus.Published) => true,
            (SurveyStatus.Published, SurveyStatus.Closed) => true,
            (_, _) when current == next => true,
            (SurveyStatus.Rejected, SurveyStatus.Draft) => true,
            (SurveyStatus.Rejected, SurveyStatus.PendingApproval) => true,
            _ => false
        };

    public async Task<Result<SurveyAnalyticsDto>> GetAnalyticsAsync(Guid surveyId, CancellationToken cancellationToken = default)
    {
        var exists = await _db.Surveys.AsNoTracking().AnyAsync(s => s.Id == surveyId, cancellationToken).ConfigureAwait(false);
        if (!exists)
            return Result<SurveyAnalyticsDto>.Fail("Survey was not found.", QuestionnaireErrors.SurveyNotFound);

        var total = await _db.SurveyResponses.CountAsync(r => r.SurveyId == surveyId, cancellationToken).ConfigureAwait(false);
        var submitted = await _db.SurveyResponses.CountAsync(
            r => r.SurveyId == surveyId && r.Status == ResponseStatus.Submitted,
            cancellationToken).ConfigureAwait(false);
        var inProg = await _db.SurveyResponses.CountAsync(
            r => r.SurveyId == surveyId && r.Status == ResponseStatus.InProgress,
            cancellationToken).ConfigureAwait(false);
        var qCount = await _db.Questions.CountAsync(q => q.SurveyId == surveyId, cancellationToken).ConfigureAwait(false);
        var pCount = await _db.SurveyParticipants.CountAsync(p => p.SurveyId == surveyId, cancellationToken).ConfigureAwait(false);

        return Result<SurveyAnalyticsDto>.Ok(new SurveyAnalyticsDto
        {
            SurveyId = surveyId,
            TotalResponses = total,
            SubmittedResponses = submitted,
            InProgressResponses = inProg,
            QuestionCount = qCount,
            ParticipantCount = pCount
        });
    }

    public async Task<Result<SurveyAnalyticsSummaryDto>> GetAnalyticsSummaryAsync(Guid surveyId, CancellationToken cancellationToken = default)
    {
        var analytics = await GetAnalyticsAsync(surveyId, cancellationToken).ConfigureAwait(false);
        if (!analytics.IsSuccess) return Result<SurveyAnalyticsSummaryDto>.Fail(analytics.Errors, analytics.FailureCode);

        var a = analytics.Value!;
        var invited = a.ParticipantCount;
        var rate = invited <= 0 ? 0 : Math.Round(100.0 * a.SubmittedResponses / invited, 2);
        return Result<SurveyAnalyticsSummaryDto>.Ok(new SurveyAnalyticsSummaryDto
        {
            SurveyId = surveyId,
            CompletionRate = rate,
            SubmittedCount = a.SubmittedResponses,
            InvitedParticipants = invited
        });
    }

    public async Task<Result<PagedResult<QuestionAnalyticsItemDto>>> GetQuestionAnalyticsAsync(
        Guid surveyId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var exists = await _db.Surveys.AsNoTracking().AnyAsync(s => s.Id == surveyId, cancellationToken).ConfigureAwait(false);
        if (!exists)
        {
            return Result<PagedResult<QuestionAnalyticsItemDto>>.Fail(
                "Survey was not found.",
                QuestionnaireErrors.SurveyNotFound);
        }

        page = page <= 0 ? PaginationConstants.DefaultPage : page;
        pageSize = pageSize <= 0 ? PaginationConstants.DefaultPageSize : pageSize;
        if (pageSize > PaginationConstants.MaxPageSize) pageSize = PaginationConstants.MaxPageSize;

        var questions = await _db.Questions.AsNoTracking()
            .Where(q => q.SurveyId == surveyId)
            .OrderBy(q => q.DisplayOrder)
            .Select(q => new { q.Id, q.TitleEn })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var counts = await (
            from a in _db.QuestionAnswers.AsNoTracking()
            join q in _db.Questions.AsNoTracking() on a.QuestionId equals q.Id
            where q.SurveyId == surveyId
            group a by a.QuestionId
            into g
            select new { QuestionId = g.Key, Cnt = g.Count() }).ToDictionaryAsync(
            x => x.QuestionId,
            x => x.Cnt,
            cancellationToken).ConfigureAwait(false);

        var fullList = questions
            .Select(q => new QuestionAnalyticsItemDto
            {
                QuestionId = q.Id,
                TitleEn = q.TitleEn,
                AnswerCount = counts.GetValueOrDefault(q.Id, 0)
            })
            .ToList();

        var totalCount = fullList.Count;
        var items = fullList
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        return Result<PagedResult<QuestionAnalyticsItemDto>>.Ok(new PagedResult<QuestionAnalyticsItemDto>
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount
        });
    }

    public async Task<Result<PagedResult<SurveyListItemDto>>> GetPendingApprovalPagedAsync(
        SurveyFilterRequest request,
        CancellationToken cancellationToken = default)
    {
        var clone = new SurveyFilterRequest
        {
            Page = request.Page,
            PageSize = request.PageSize,
            Search = request.Search,
            Status = SurveyStatus.PendingApproval
        };
        return await GetPagedAsync(clone, cancellationToken).ConfigureAwait(false);
    }

    public async Task<Result<int>> CloseExpiredPublishedSurveysAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var ids = await _db.Surveys.AsNoTracking()
            .Where(s =>
                s.Status == SurveyStatus.Published &&
                s.ClosesAtUtc != null &&
                s.ClosesAtUtc <= now)
            .Select(s => s.Id)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var closed = 0;
        foreach (var id in ids)
        {
            var r = await CloseAsync(id, cancellationToken).ConfigureAwait(false);
            if (r.IsSuccess)
                closed++;
        }

        return Result<int>.Ok(closed);
    }

    public async Task<Result<SurveyNumericAnalyticsDto>> GetNumericQuestionAnalyticsAsync(
        Guid surveyId,
        CancellationToken cancellationToken = default)
    {
        var exists = await _db.Surveys.AsNoTracking().AnyAsync(s => s.Id == surveyId, cancellationToken).ConfigureAwait(false);
        if (!exists)
            return Result<SurveyNumericAnalyticsDto>.Fail("Survey was not found.", QuestionnaireErrors.SurveyNotFound);

        QuestionType[] numericTypes =
        [
            QuestionType.Rating,
            QuestionType.Scale,
            QuestionType.Number,
            QuestionType.YesNo
        ];

        var questions = await _db.Questions.AsNoTracking()
            .Where(q => q.SurveyId == surveyId && numericTypes.Contains(q.Type))
            .OrderBy(q => q.DisplayOrder)
            .Select(q => new { q.Id, q.TitleAr, q.TitleEn, q.Type })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var stats = new List<NumericQuestionStatDto>();
        foreach (var q in questions)
        {
            var jsonValues = await (
                from a in _db.QuestionAnswers.AsNoTracking()
                join r in _db.SurveyResponses.AsNoTracking() on a.ResponseId equals r.Id
                where a.QuestionId == q.Id && r.SurveyId == surveyId && r.Status == ResponseStatus.Submitted
                select a.ValueJson).ToListAsync(cancellationToken).ConfigureAwait(false);

            var values = new List<double>();
            foreach (var json in jsonValues)
            {
                if (TryExtractNumeric(json, out var v))
                    values.Add(v);
            }

            stats.Add(new NumericQuestionStatDto
            {
                QuestionId = q.Id,
                TitleAr = q.TitleAr,
                TitleEn = q.TitleEn,
                Type = q.Type,
                AnswerCount = values.Count,
                Average = values.Count == 0 ? null : values.Average(),
                Min = values.Count == 0 ? null : values.Min(),
                Max = values.Count == 0 ? null : values.Max()
            });
        }

        return Result<SurveyNumericAnalyticsDto>.Ok(new SurveyNumericAnalyticsDto
        {
            SurveyId = surveyId,
            Questions = stats
        });
    }

    private async Task<SurveyDetailDto> MapDetailAsync(Survey s, CancellationToken cancellationToken)
    {
        string? ownerDisplayName = null;
        if (s.OwnerUserId is { } ownerId)
        {
            var ownerRow = await _db.Users.AsNoTracking()
                .Where(u => u.Id == ownerId)
                .Select(u => new { u.NameAr, u.NameEn, u.UserName })
                .FirstOrDefaultAsync(cancellationToken)
                .ConfigureAwait(false);
            if (ownerRow is not null)
                ownerDisplayName = UserDisplayNames.Format(ownerRow.NameAr, ownerRow.NameEn, ownerRow.UserName);
        }

        return new SurveyDetailDto
        {
            Id = s.Id,
            TitleAr = s.TitleAr,
            TitleEn = s.TitleEn,
            DescriptionAr = s.DescriptionAr,
            DescriptionEn = s.DescriptionEn,
            Code = s.Code,
            Status = s.Status,
            AudienceScope = s.AudienceScope,
            Version = s.Version,
            OwnerUserId = s.OwnerUserId,
            OwnerDisplayName = ownerDisplayName,
            TemplateId = s.TemplateId,
            PublishedAtUtc = s.PublishedAtUtc,
            ClosedAtUtc = s.ClosedAtUtc,
            OpensAtUtc = s.OpensAtUtc,
            ClosesAtUtc = s.ClosesAtUtc,
            RejectionReason = s.RejectionReason
        };
    }

    private async Task ApplyPublishAudienceAsync(
        Guid surveyId,
        PublishSurveyRequest? pub,
        CancellationToken cancellationToken)
    {
        if (pub is null)
            return;

        var s = await _db.Surveys.FirstOrDefaultAsync(x => x.Id == surveyId, cancellationToken).ConfigureAwait(false);
        if (s is null)
            return;

        if (pub.AudienceScope is { } ascope)
            s.AudienceScope = ascope;

        if (pub.AudienceUserIds is not null)
        {
            var existing = await _db.SurveyAudienceMembers
                .Where(m => m.SurveyId == surveyId)
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);
            _db.SurveyAudienceMembers.RemoveRange(existing);

            if (s.AudienceScope == SurveyAudienceScope.SpecificUsers)
            {
                foreach (var uid in pub.AudienceUserIds.Distinct())
                {
                    _db.SurveyAudienceMembers.Add(new SurveyAudienceMember { SurveyId = surveyId, UserId = uid });
                }
            }
        }
        else if (pub.AudienceScope is not null && s.AudienceScope != SurveyAudienceScope.SpecificUsers)
        {
            var existing = await _db.SurveyAudienceMembers
                .Where(m => m.SurveyId == surveyId)
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);
            _db.SurveyAudienceMembers.RemoveRange(existing);
        }

        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    private static void AppendOwnerQuestions(Survey survey, IReadOnlyList<CreateSurveyQuestionItem>? items)
    {
        if (items is null || items.Count == 0)
            return;

        var cursor = survey.Questions.Count == 0 ? 0 : survey.Questions.Max(q => q.DisplayOrder);
        foreach (var item in items)
        {
            int disp;
            if (item.DisplayOrder is { } explicitOrder)
            {
                disp = explicitOrder;
                cursor = Math.Max(cursor, explicitOrder);
            }
            else
            {
                disp = ++cursor;
            }

            survey.Questions.Add(new Question
            {
                Survey = survey,
                DisplayOrder = disp,
                Type = item.Type,
                TitleAr = item.TitleAr.Trim(),
                TitleEn = item.TitleEn.Trim(),
                HelpTextAr = string.IsNullOrWhiteSpace(item.HelpTextAr) ? null : item.HelpTextAr.Trim(),
                HelpTextEn = string.IsNullOrWhiteSpace(item.HelpTextEn) ? null : item.HelpTextEn.Trim(),
                IsRequired = item.IsRequired,
                OptionsJson = string.IsNullOrWhiteSpace(item.OptionsJson) ? null : item.OptionsJson
            });
        }
    }

    private static bool TryExtractNumeric(string json, out double value)
    {
        value = 0;
        try
        {
            using var doc = JsonDocument.Parse(string.IsNullOrWhiteSpace(json) ? "{}" : json);
            var root = doc.RootElement;
            if (root.ValueKind == JsonValueKind.Number && root.TryGetDouble(out var d))
            {
                value = d;
                return true;
            }

            if (root.TryGetProperty("value", out var prop) && prop.TryGetDouble(out var dv))
            {
                value = dv;
                return true;
            }

            if (root.ValueKind == JsonValueKind.String &&
                double.TryParse(root.GetString(), System.Globalization.NumberStyles.Any,
                    System.Globalization.CultureInfo.InvariantCulture, out var ds))
            {
                value = ds;
                return true;
            }

            if (root.ValueKind == JsonValueKind.True)
            {
                value = 1;
                return true;
            }

            if (root.ValueKind == JsonValueKind.False)
            {
                value = 0;
                return true;
            }
        }
        catch
        {
            return false;
        }

        return false;
    }

    private static void SoftDeleteEntity(Domain.Common.AuditableDomainEntity e)
    {
        e.RecordStatus = RecordStatus.Deleted;
    }

    private static void TryApplyTemplateStructure(Survey survey, string structureJson)
    {
        try
        {
            using var doc = JsonDocument.Parse(string.IsNullOrWhiteSpace(structureJson) ? "{}" : structureJson);
            if (doc.RootElement.TryGetProperty("questions", out var arr) && arr.ValueKind == JsonValueKind.Array)
            {
                var order = 0;
                foreach (var el in arr.EnumerateArray())
                {
                    order++;
                    var titleAr = el.TryGetProperty("titleAr", out var tar) ? tar.GetString() ?? "سؤال" : "سؤال";
                    var titleEn = el.TryGetProperty("titleEn", out var ten) ? ten.GetString() ?? "Question" : "Question";
                    var typeVal = QuestionType.ShortText;
                    if (el.TryGetProperty("type", out var tv) && tv.ValueKind == JsonValueKind.Number && tv.TryGetInt32(out var ti))
                        typeVal = (QuestionType)ti;

                    var options = el.TryGetProperty("optionsJson", out var oj) ? oj.GetRawText() : null;
                    survey.Questions.Add(new Question
                    {
                        Survey = survey,
                        DisplayOrder = el.TryGetProperty("displayOrder", out var d) && d.TryGetInt32(out var di) ? di : order,
                        Type = typeVal,
                        TitleAr = titleAr,
                        TitleEn = titleEn,
                        HelpTextAr = el.TryGetProperty("helpTextAr", out var ha) ? ha.GetString() : null,
                        HelpTextEn = el.TryGetProperty("helpTextEn", out var he) ? he.GetString() : null,
                        IsRequired = el.TryGetProperty("isRequired", out var ir) && ir.ValueKind == JsonValueKind.True,
                        OptionsJson = options
                    });
                }
            }
        }
        catch
        {
            /* ignore malformed template JSON */
        }
    }
}
