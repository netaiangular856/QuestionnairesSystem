using System.Text.Json;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using QuestionnairesSystem.Application.Common;
using QuestionnairesSystem.Application.Features.Identity;
using QuestionnairesSystem.Application.Features.Notifications;
using QuestionnairesSystem.Application.Features.Notifications.DTOs;
using QuestionnairesSystem.Application.Features.Notifications.Interfaces;
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
    private readonly IInboxNotificationDispatchService _notify;

    public SurveyService(
        QuestionnairesDbContext db,
        ICurrentUserService currentUser,
        IValidator<CreateSurveyRequest> createValidator,
        IValidator<UpdateSurveyRequest> updateValidator,
        IValidator<SurveyFilterRequest> filterValidator,
        IValidator<RejectSurveyRequest> rejectValidator,
        IValidator<PatchSurveyStatusRequest> patchStatusValidator,
        IValidator<PublishSurveyRequest> publishValidator,
        IInboxNotificationDispatchService notify)
    {
        _db = db;
        _currentUser = currentUser;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
        _filterValidator = filterValidator;
        _rejectValidator = rejectValidator;
        _patchStatusValidator = patchStatusValidator;
        _publishValidator = publishValidator;
        _notify = notify;
    }

    public async Task<Result<SurveyDetailDto>> CreateAsync(CreateSurveyRequest request, CancellationToken cancellationToken = default)
    {
        var validation = await _createValidator.ValidateAsync(request, cancellationToken).ConfigureAwait(false);
        if (!validation.IsValid)
            return Result<SurveyDetailDto>.Fail(validation.ToErrorMessages());

        var audienceRef = await ValidateAudienceMemberInputsAsync(request.AudienceMembers, cancellationToken)
            .ConfigureAwait(false);
        if (!audienceRef.IsSuccess)
            return Result<SurveyDetailDto>.Fail(audienceRef.Errors, audienceRef.FailureCode);

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

        if (request.AudienceScope == SurveyAudienceScope.SpecificUsers && request.AudienceMembers is { Count: > 0 })
        {
            foreach (var m in NormalizeAudiencePayload(request.AudienceMembers))
            {
                if (m.UserId is { } uid)
                    survey.AudienceMembers.Add(new SurveyAudienceMember { UserId = uid });
                else if (!string.IsNullOrWhiteSpace(m.Email))
                    survey.AudienceMembers.Add(new SurveyAudienceMember { Email = NormalizeAudienceEmail(m.Email) });
            }
        }

        _db.Surveys.Add(survey);
        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        if (_currentUser.UserId is { } creatorId)
        {
            var (na, ne) = SurveyNamePair(survey);
            var text = survey.TemplateId is null
                ? new LocalizedInboxNotificationText(
                    "تم إنشاء استبيان",
                    "Survey created",
                    $"تم إنشاء الاستبيان «{na}».",
                    $"Survey «{ne}» was created.")
                : new LocalizedInboxNotificationText(
                    "تم إنشاء استبيان من قالب",
                    "Survey created from template",
                    $"تم إنشاء الاستبيان «{na}» من قالب.",
                    $"Survey «{ne}» was created from a template.");
            await _notify.DispatchAsync(
                new[] { creatorId },
                text,
                NotificationRelatedEntityTypes.Survey,
                survey.Id,
                null,
                cancellationToken).ConfigureAwait(false);
        }

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

        var items = await MapListItemsAsync(ids, cancellationToken).ConfigureAwait(false);

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
            .Include(x => x.AudienceMembers)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken).ConfigureAwait(false);
        if (s is null)
            return Result<SurveyDetailDto>.Fail("Survey was not found.", QuestionnaireErrors.SurveyNotFound);

        s.TitleAr = request.TitleAr.Trim();
        s.TitleEn = request.TitleEn.Trim();
        s.DescriptionAr = string.IsNullOrWhiteSpace(request.DescriptionAr) ? null : request.DescriptionAr.Trim();
        s.DescriptionEn = string.IsNullOrWhiteSpace(request.DescriptionEn) ? null : request.DescriptionEn.Trim();
        s.Code = string.IsNullOrWhiteSpace(request.Code) ? null : request.Code.Trim();
        s.AudienceScope = request.AudienceScope;

        if (request.AudienceScope != SurveyAudienceScope.SpecificUsers)
        {
            if (s.AudienceMembers.Count > 0)
            {
                _db.SurveyAudienceMembers.RemoveRange(s.AudienceMembers);
                s.AudienceMembers.Clear();
            }
        }
        else if (request.AudienceMembers is not null)
        {
            var audienceRef = await ValidateAudienceMemberInputsAsync(request.AudienceMembers, cancellationToken)
                .ConfigureAwait(false);
            if (!audienceRef.IsSuccess)
                return Result<SurveyDetailDto>.Fail(audienceRef.Errors, audienceRef.FailureCode);

            _db.SurveyAudienceMembers.RemoveRange(s.AudienceMembers);
            s.AudienceMembers.Clear();
            foreach (var m in NormalizeAudiencePayload(request.AudienceMembers))
            {
                if (m.UserId is { } uid)
                    s.AudienceMembers.Add(new SurveyAudienceMember { SurveyId = s.Id, UserId = uid });
                else if (!string.IsNullOrWhiteSpace(m.Email))
                    s.AudienceMembers.Add(new SurveyAudienceMember { SurveyId = s.Id, Email = NormalizeAudienceEmail(m.Email) });
            }
        }

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

        var updateRecipients = new HashSet<Guid>();
        if (_currentUser.UserId is { } actorUpd)
            updateRecipients.Add(actorUpd);
        if (s.OwnerUserId is { } ownerUpd)
            updateRecipients.Add(ownerUpd);
        var (naUpd, neUpd) = SurveyNamePair(s);
        await _notify.DispatchAsync(
            updateRecipients,
            new LocalizedInboxNotificationText(
                "تم تحديث الاستبيان",
                "Survey updated",
                $"تم تحديث الاستبيان «{naUpd}».",
                $"Survey «{neUpd}» was updated."),
            NotificationRelatedEntityTypes.Survey,
            s.Id,
            null,
            cancellationToken).ConfigureAwait(false);

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

        var delRecipients = new HashSet<Guid>();
        if (_currentUser.UserId is { } actorDel)
            delRecipients.Add(actorDel);
        if (s.OwnerUserId is { } ownerDel)
            delRecipients.Add(ownerDel);
        var (naDel, neDel) = SurveyNamePair(s);
        await _notify.DispatchAsync(
            delRecipients,
            new LocalizedInboxNotificationText(
                "تم حذف الاستبيان",
                "Survey deleted",
                $"تم حذف الاستبيان «{naDel}» (أرشفة).",
                $"Survey «{neDel}» was deleted (soft)."),
            NotificationRelatedEntityTypes.Survey,
            s.Id,
            null,
            cancellationToken).ConfigureAwait(false);

        return Result.Ok();
    }

    public async Task<Result<SurveyDetailDto>> DuplicateAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var source = await _db.Surveys
            .Include(x => x.Questions)
            .Include(x => x.AudienceMembers)
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

        foreach (var m in source.AudienceMembers)
        {
            copy.AudienceMembers.Add(new SurveyAudienceMember { UserId = m.UserId, Email = m.Email });
        }

        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        if (_currentUser.UserId is { } dupActor)
        {
            var (naDup, neDup) = SurveyNamePair(copy);
            await _notify.DispatchAsync(
                new[] { dupActor },
                new LocalizedInboxNotificationText(
                    "تم نسخ الاستبيان",
                    "Survey duplicated",
                    $"تم إنشاء نسخة من الاستبيان «{naDup}».",
                    $"A copy of survey «{neDup}» was created."),
                NotificationRelatedEntityTypes.Survey,
                copy.Id,
                null,
                cancellationToken).ConfigureAwait(false);
        }

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

            if (publishRequest.AudienceMembers is { Count: > 0 })
            {
                var av = await ValidateAudienceMemberInputsAsync(publishRequest.AudienceMembers, cancellationToken)
                    .ConfigureAwait(false);
                if (!av.IsSuccess)
                    return Result<SurveyDetailDto>.Fail(av.Errors, av.FailureCode);
            }
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

        var previousStatus = s.Status;
        if (!IsTransitionAllowed(previousStatus, target))
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

        await NotifySurveyLifecycleAsync(s, previousStatus, target, rejectionReason, cancellationToken)
            .ConfigureAwait(false);

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
        // Invited completion = distinct participants with a submitted response / invited (not raw response count).
        var participantsSubmitted = await _db.SurveyResponses.AsNoTracking()
            .Where(r => r.SurveyId == surveyId && r.Status == ResponseStatus.Submitted && r.ParticipantId != null)
            .Select(r => r.ParticipantId!.Value)
            .Distinct()
            .CountAsync(cancellationToken)
            .ConfigureAwait(false);

        double rate;
        if (invited > 0)
        {
            rate = Math.Min(100.0, Math.Round(100.0 * participantsSubmitted / invited, 2));
        }
        else
        {
            var started = a.SubmittedResponses + a.InProgressResponses;
            rate = started > 0 ? Math.Round(100.0 * a.SubmittedResponses / started, 2) : 0;
        }

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
        var validation = await _filterValidator.ValidateAsync(request, cancellationToken).ConfigureAwait(false);
        if (!validation.IsValid)
            return Result<PagedResult<SurveyListItemDto>>.Fail(validation.ToErrorMessages());

        var page = request.Page <= 0 ? PaginationConstants.DefaultPage : request.Page;
        var pageSize = request.PageSize <= 0 ? PaginationConstants.DefaultPageSize : request.PageSize;
        if (pageSize > PaginationConstants.MaxPageSize) pageSize = PaginationConstants.MaxPageSize;

        var q = _db.Surveys.AsNoTracking()
            .Where(s => s.Status == SurveyStatus.PendingApproval);

        if (!string.IsNullOrWhiteSpace(request.Search))
        {            var term = request.Search.Trim();
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

        var items = await MapListItemsAsync(ids, cancellationToken).ConfigureAwait(false);

        return Result<PagedResult<SurveyListItemDto>>.Ok(new PagedResult<SurveyListItemDto>
        {            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalCount = total
        });
    }

    public async Task<Result<PagedResult<SurveyListItemDto>>> GetAvailableForParticipationPagedAsync(
        SurveyFilterRequest request,
        CancellationToken cancellationToken = default)
    {
        var validation = await _filterValidator.ValidateAsync(request, cancellationToken).ConfigureAwait(false);
        if (!validation.IsValid)
            return Result<PagedResult<SurveyListItemDto>>.Fail(validation.ToErrorMessages());

        var page = request.Page <= 0 ? PaginationConstants.DefaultPage : request.Page;
        var pageSize = request.PageSize <= 0 ? PaginationConstants.DefaultPageSize : request.PageSize;
        if (pageSize > PaginationConstants.MaxPageSize) pageSize = PaginationConstants.MaxPageSize;

        var now = DateTime.UtcNow;
        var userId = _currentUser.UserId;
        string? userEmailNorm = null;
        if (userId is { } uidForAudience)
        {
            var em = await _db.Users.AsNoTracking()
                .Where(u => u.Id == uidForAudience)
                .Select(u => u.Email)
                .FirstOrDefaultAsync(cancellationToken)
                .ConfigureAwait(false);
            userEmailNorm = string.IsNullOrWhiteSpace(em) ? null : em.Trim().ToLowerInvariant();
        }

        // Base filter: Published, not closed, and within time window (if set)
        var q = _db.Surveys.AsNoTracking()
            .Where(s => s.Status == SurveyStatus.Published)
            .Where(s => s.ClosedAtUtc == null)
            .Where(s => s.OpensAtUtc == null || s.OpensAtUtc <= now)
            .Where(s => s.ClosesAtUtc == null || s.ClosesAtUtc >= now);

        // Audience filtering
        q = q.Where(s =>
            s.AudienceScope == SurveyAudienceScope.Everyone ||
            s.AudienceScope == SurveyAudienceScope.AllOrganizationMembers ||
            (s.AudienceScope == SurveyAudienceScope.SpecificUsers &&
                ((userId != null && s.AudienceMembers.Any(m => m.UserId == userId)) ||
                 (userEmailNorm != null &&
                  s.AudienceMembers.Any(m => m.Email != null && m.Email.ToLower() == userEmailNorm)))));

        if (!string.IsNullOrWhiteSpace(request.Search))
        {            var term = request.Search.Trim();
            q = q.Where(s =>
                s.TitleAr.Contains(term) ||
                s.TitleEn.Contains(term) ||
                (s.Code != null && s.Code.Contains(term)));
        }

        var total = await q.CountAsync(cancellationToken).ConfigureAwait(false);
        var ids = await q
            .OrderByDescending(s => s.PublishedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(s => s.Id)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var items = await MapListItemsAsync(ids, cancellationToken).ConfigureAwait(false);

        return Result<PagedResult<SurveyListItemDto>>.Ok(new PagedResult<SurveyListItemDto>
        {            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalCount = total
        });
    }

    private async Task<List<SurveyListItemDto>> MapListItemsAsync(IEnumerable<Guid> ids, CancellationToken cancellationToken)
    {        var items = new List<SurveyListItemDto>();
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
        return items;
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

    public async Task<Result<SurveyComprehensiveAnalyticsDto>> GetComprehensiveAnalyticsAsync(
        Guid surveyId,
        CancellationToken cancellationToken = default)
    {
        var survey = await _db.Surveys.AsNoTracking()
            .Where(s => s.Id == surveyId)
            .Select(s => new { s.Id, s.TitleAr, s.TitleEn })
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);

        if (survey is null)
            return Result<SurveyComprehensiveAnalyticsDto>.Fail("Survey was not found.", QuestionnaireErrors.SurveyNotFound);

        // Get overview analytics
        var totalParticipants = await _db.SurveyParticipants.AsNoTracking()
            .CountAsync(p => p.SurveyId == surveyId, cancellationToken)
            .ConfigureAwait(false);

        var responses = await _db.SurveyResponses.AsNoTracking()
            .Where(r => r.SurveyId == surveyId)
            .GroupBy(r => r.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var submitted = responses.FirstOrDefault(r => r.Status == ResponseStatus.Submitted)?.Count ?? 0;
        var inProgress = responses.FirstOrDefault(r => r.Status == ResponseStatus.InProgress)?.Count ?? 0;

        var participantsWithSubmitted = await _db.SurveyResponses.AsNoTracking()
            .Where(r => r.SurveyId == surveyId && r.Status == ResponseStatus.Submitted && r.ParticipantId != null)
            .Select(r => r.ParticipantId!.Value)
            .Distinct()
            .CountAsync(cancellationToken)
            .ConfigureAwait(false);

        var totalQuestions = await _db.Questions.AsNoTracking()
            .CountAsync(q => q.SurveyId == surveyId, cancellationToken)
            .ConfigureAwait(false);

        double completionRate;
        if (totalParticipants > 0)
        {
            completionRate = Math.Min(100.0, (double)participantsWithSubmitted / totalParticipants * 100.0);
        }
        else
        {
            var started = submitted + inProgress;
            completionRate = started > 0 ? (double)submitted / started * 100.0 : 0;
        }

        var overview = new SurveyOverviewAnalytics
        {
            TotalParticipants = totalParticipants,
            SubmittedResponses = submitted,
            InProgressResponses = inProgress,
            CompletionRate = completionRate,
            TotalQuestions = totalQuestions
        };

        // Get response timeline (last 30 days)
        var thirtyDaysAgo = DateTime.UtcNow.AddDays(-30);
        var submittedDates = await _db.SurveyResponses.AsNoTracking()
            .Where(r => r.SurveyId == surveyId && r.Status == ResponseStatus.Submitted && r.SubmittedAtUtc >= thirtyDaysAgo)
            .Select(r => r.SubmittedAtUtc)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var timeline = submittedDates
            .Where(date => date.HasValue)
            .GroupBy(date => DateTime.SpecifyKind(date.Value.Date, DateTimeKind.Utc))
            .Select(g => new { Date = g.Key, Count = g.Count() })
            .OrderBy(x => x.Date)
            .ToList();

        var responseTimeline = timeline.Select(t => new ResponseTimelineAnalytics
        {
            Date = t.Date.ToString("yyyy-MM-dd"),
            ResponseCount = t.Count
        }).ToList();

        // Get question analytics
        var questions = await _db.Questions.AsNoTracking()
            .Where(q => q.SurveyId == surveyId)
            .OrderBy(q => q.DisplayOrder)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var questionAnalytics = new List<QuestionAnalyticsDto>();
        var textAnswerCorpus = new List<string>();
        foreach (var question in questions)
        {
            var answers = await _db.QuestionAnswers.AsNoTracking()
                .Join(_db.SurveyResponses.AsNoTracking(), a => a.ResponseId, r => r.Id, (a, r) => new { a, r })
                .Where(x => x.a.QuestionId == question.Id && x.r.SurveyId == surveyId && x.r.Status == ResponseStatus.Submitted)
                .Select(x => x.a.ValueJson)
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

            var questionType = question.Type.ToString();
            var answerDistribution = new List<AnswerDistributionDto>();
            double? averageRating = null;
            double? minRating = null;
            double? maxRating = null;

            if (question.Type == QuestionType.Rating || question.Type == QuestionType.Scale)
            {
                var numericValues = new List<double>();
                foreach (var answer in answers)
                {
                    if (double.TryParse(answer, out var value))
                    {
                        numericValues.Add(value);
                    }
                }

                if (numericValues.Any())
                {
                    averageRating = numericValues.Average();
                    minRating = numericValues.Min();
                    maxRating = numericValues.Max();

                    // Create rating distribution
                    var ratingGroups = numericValues.GroupBy(v => (int)v)
                        .Select(g => new { Rating = g.Key, Count = g.Count() })
                        .OrderBy(x => x.Rating);

                    answerDistribution = ratingGroups.Select(g => new AnswerDistributionDto
                    {
                        OptionText = g.Rating.ToString(),
                        OptionTextAr = g.Rating.ToString(),
                        OptionTextEn = g.Rating.ToString(),
                        Count = g.Count,
                        Percentage = answers.Count > 0 ? (double)g.Count / answers.Count * 100 : 0
                    }).ToList();
                }
            }
            else if (question.Type == QuestionType.MultipleChoice || question.Type == QuestionType.SingleChoice)
            {
                var labelMap = ParseChoiceOptionsMap(question.OptionsJson);
                var optionCounts = new Dictionary<string, int>(StringComparer.Ordinal);

                foreach (var raw in answers.Where(a => !string.IsNullOrWhiteSpace(a)))
                {
                    if (question.Type == QuestionType.SingleChoice)
                    {
                        var val = TryDeserializeChoiceValue(raw);
                        if (string.IsNullOrEmpty(val))
                            continue;
                        optionCounts.TryGetValue(val, out var n);
                        optionCounts[val] = n + 1;
                    }
                    else
                    {
                        foreach (var val in TryDeserializeChoiceArray(raw))
                        {
                            if (string.IsNullOrEmpty(val))
                                continue;
                            optionCounts.TryGetValue(val, out var n);
                            optionCounts[val] = n + 1;
                        }
                    }
                }

                answerDistribution = optionCounts
                    .OrderByDescending(kv => kv.Value)
                    .Select(kv =>
                    {
                        labelMap.TryGetValue(kv.Key, out var labels);
                        var ar = string.IsNullOrWhiteSpace(labels.Ar) ? kv.Key : labels.Ar;
                        var en = string.IsNullOrWhiteSpace(labels.En) ? kv.Key : labels.En;
                        return new AnswerDistributionDto
                        {
                            OptionText = string.IsNullOrWhiteSpace(en) ? kv.Key : en,
                            OptionTextAr = ar,
                            OptionTextEn = en,
                            Count = kv.Value,
                            Percentage = answers.Count > 0 ? (double)kv.Value / answers.Count * 100 : 0
                        };
                    })
                    .ToList();
            }
            else if (question.Type == QuestionType.YesNo)
            {
                var yesCount = answers.Count(a => a?.ToLower() == "true");
                var noCount = answers.Count(a => a?.ToLower() == "false");

                answerDistribution = new List<AnswerDistributionDto>
                {
                    new()
                    {
                        OptionText = "Yes",
                        OptionTextAr = "نعم",
                        OptionTextEn = "Yes",
                        Count = yesCount,
                        Percentage = answers.Count > 0 ? (double)yesCount / answers.Count * 100 : 0
                    },
                    new()
                    {
                        OptionText = "No",
                        OptionTextAr = "لا",
                        OptionTextEn = "No",
                        Count = noCount,
                        Percentage = answers.Count > 0 ? (double)noCount / answers.Count * 100 : 0
                    }
                };
            }
            else if (question.Type is QuestionType.ShortText or QuestionType.LongText)
            {
                foreach (var raw in answers)
                {
                    var plain = TextAnswerKeywordAggregator.UnwrapJsonAnswerText(raw);
                    if (!string.IsNullOrWhiteSpace(plain))
                    {
                        textAnswerCorpus.Add(plain);
                    }
                }
            }

            questionAnalytics.Add(new QuestionAnalyticsDto
            {
                QuestionId = question.Id,
                TitleAr = question.TitleAr,
                TitleEn = question.TitleEn,
                QuestionType = questionType,
                TotalAnswers = answers.Count,
                AnswerDistribution = answerDistribution,
                AverageRating = averageRating,
                MinRating = minRating,
                MaxRating = maxRating
            });
        }

        // Get category analytics (simplified - based on question types)
        var categoryGroups = questionAnalytics.GroupBy(q => q.QuestionType)
            .Select(g => new { Category = g.Key, Count = g.Sum(q => q.TotalAnswers) })
            .OrderByDescending(x => x.Count);

        var categories = categoryGroups.Select(g => new CategoryAnalyticsDto
        {
            CategoryName = g.Category,
            ResponseCount = g.Count,
            Percentage = questionAnalytics.Sum(q => q.TotalAnswers) > 0 ? 
                (double)g.Count / questionAnalytics.Sum(q => q.TotalAnswers) * 100 : 0
        }).ToList();

        // Get overall rating analytics
        var allRatingQuestions = questionAnalytics.Where(q => 
            q.QuestionType == "Rating" || q.QuestionType == "Scale").ToList();
        
        var ratings = new List<RatingAnalyticsDto>();
        if (allRatingQuestions.Any())
        {
            var allRatings = allRatingQuestions.SelectMany(q => q.AnswerDistribution)
                .Where(ad => int.TryParse(ad.OptionText, out _))
                .ToList();

            var ratingGroups = allRatings.GroupBy(ad => int.Parse(ad.OptionText))
                .Select(g => new { Rating = g.Key, Count = g.Sum(ad => ad.Count) })
                .OrderBy(x => x.Rating);

            var totalRatingResponses = ratingGroups.Sum(g => g.Count);
            ratings = ratingGroups.Select(g => new RatingAnalyticsDto
            {
                Rating = g.Rating,
                Count = g.Count,
                Percentage = totalRatingResponses > 0 ? (double)g.Count / totalRatingResponses * 100 : 0
            }).ToList();
        }

        var textAnswerKeywords = TextAnswerKeywordAggregator.Aggregate(textAnswerCorpus);

        return Result<SurveyComprehensiveAnalyticsDto>.Ok(new SurveyComprehensiveAnalyticsDto
        {
            SurveyId = surveyId,
            SurveyTitle = survey.TitleEn,
            Overview = overview,
            ResponseTimeline = responseTimeline,
            Questions = questionAnalytics,
            Categories = categories,
            Ratings = ratings,
            TextAnswerKeywords = textAnswerKeywords
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

        var memberRows = await (
                from m in _db.SurveyAudienceMembers.AsNoTracking()
                where m.SurveyId == s.Id
                join u in _db.Users.AsNoTracking() on m.UserId equals u.Id into ug
                from u in ug.DefaultIfEmpty()
                select new { m.UserId, m.Email, u.NameAr, u.NameEn, u.UserName })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var audienceMembers = memberRows.ConvertAll(r => new SurveyAudienceMemberDetailDto
        {
            UserId = r.UserId,
            Email = r.Email,
            DisplayName = r.UserId is not null && !string.IsNullOrEmpty(r.UserName)
                ? UserDisplayNames.Format(r.NameAr, r.NameEn, r.UserName!)
                : (!string.IsNullOrWhiteSpace(r.Email) ? r.Email!.Trim() : "—")
        });

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
            RejectionReason = s.RejectionReason,
            AudienceMembers = audienceMembers
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

        var shouldReplaceAudienceRows =
            pub.AudienceMembers is not null
            || pub.AudienceUserIds is not null
            || (pub.AudienceScope is not null && pub.AudienceScope != SurveyAudienceScope.SpecificUsers);

        if (shouldReplaceAudienceRows)
        {
            var existing = await _db.SurveyAudienceMembers
                .Where(m => m.SurveyId == surveyId)
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);
            _db.SurveyAudienceMembers.RemoveRange(existing);

            if (s.AudienceScope == SurveyAudienceScope.SpecificUsers)
            {
                if (pub.AudienceMembers is { Count: > 0 })
                {
                    foreach (var m in NormalizeAudiencePayload(pub.AudienceMembers))
                    {
                        if (m.UserId is { } uid)
                            _db.SurveyAudienceMembers.Add(new SurveyAudienceMember { SurveyId = surveyId, UserId = uid });
                        else if (!string.IsNullOrWhiteSpace(m.Email))
                            _db.SurveyAudienceMembers.Add(
                                new SurveyAudienceMember { SurveyId = surveyId, Email = NormalizeAudienceEmail(m.Email) });
                    }
                }
                else if (pub.AudienceUserIds is { Count: > 0 })
                {
                    foreach (var uid in pub.AudienceUserIds.Distinct())
                        _db.SurveyAudienceMembers.Add(new SurveyAudienceMember { SurveyId = surveyId, UserId = uid });
                }
            }
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

    private async Task NotifySurveyLifecycleAsync(
        Survey survey,
        SurveyStatus from,
        SurveyStatus to,
        string? rejectionReason,
        CancellationToken cancellationToken)
    {
        if (from == to)
            return;

        var (na, ne) = SurveyNamePair(survey);
        var surveyId = survey.Id;
        var recipients = new HashSet<Guid>();
        if (_currentUser.UserId is { } actor)
            recipients.Add(actor);
        if (survey.OwnerUserId is { } owner)
            recipients.Add(owner);

        if (to == SurveyStatus.PendingApproval)
        {
            var approvers = await _notify.GetActiveUserIdsWithAnyPermissionAsync(
                    new[]
                    {
                        PermissionCodes.SurveyApprove,
                        PermissionCodes.SurveyApprovalView
                    },
                    cancellationToken)
                .ConfigureAwait(false);
            foreach (var u in approvers)
                recipients.Add(u);

            await _notify.DispatchAsync(
                recipients,
                new LocalizedInboxNotificationText(
                    "استبيان بانتظار الاعتماد",
                    "Survey pending approval",
                    $"تم إرسال الاستبيان «{na}» لاعتماد.",
                    $"Survey «{ne}» was submitted for approval."),
                NotificationRelatedEntityTypes.Survey,
                surveyId,
                null,
                cancellationToken).ConfigureAwait(false);
            return;
        }

        if (to == SurveyStatus.Approved)
        {
            await _notify.DispatchAsync(
                recipients,
                new LocalizedInboxNotificationText(
                    "تم اعتماد الاستبيان",
                    "Survey approved",
                    $"تم اعتماد الاستبيان «{na}».",
                    $"Survey «{ne}» was approved."),
                NotificationRelatedEntityTypes.Survey,
                surveyId,
                null,
                cancellationToken).ConfigureAwait(false);
            return;
        }

        if (to == SurveyStatus.Rejected)
        {
            var reasonAr = string.IsNullOrWhiteSpace(rejectionReason) ? string.Empty : $" السبب: {rejectionReason.Trim()}";
            var reasonEn = string.IsNullOrWhiteSpace(rejectionReason) ? string.Empty : $" Reason: {rejectionReason.Trim()}";
            await _notify.DispatchAsync(
                recipients,
                new LocalizedInboxNotificationText(
                    "تم رفض الاستبيان",
                    "Survey rejected",
                    $"تم رفض الاستبيان «{na}».{reasonAr}",
                    $"Survey «{ne}» was rejected.{reasonEn}"),
                NotificationRelatedEntityTypes.Survey,
                surveyId,
                null,
                cancellationToken).ConfigureAwait(false);
            return;
        }

        if (to == SurveyStatus.Published)
        {
            var audienceUserIds = await _db.SurveyAudienceMembers.AsNoTracking()
                .Where(m => m.SurveyId == surveyId && m.UserId != null)
                .Select(m => m.UserId!.Value)
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);
            foreach (var u in audienceUserIds)
                recipients.Add(u);

            var participantUserIds = await _db.SurveyParticipants.AsNoTracking()
                .Where(p => p.SurveyId == surveyId && p.UserId != null)
                .Select(p => p.UserId!.Value)
                .Distinct()
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);
            foreach (var u in participantUserIds)
                recipients.Add(u);

            await _notify.DispatchAsync(
                recipients,
                new LocalizedInboxNotificationText(
                    "تم نشر الاستبيان",
                    "Survey published",
                    $"تم نشر الاستبيان «{na}». تم إشعار المستخدمين المدعوين الذين لديهم حسابات.",
                    $"Survey «{ne}» was published. Invited users with accounts were notified."),
                NotificationRelatedEntityTypes.Survey,
                surveyId,
                null,
                cancellationToken).ConfigureAwait(false);
            return;
        }

        if (to == SurveyStatus.Closed)
        {
            await _notify.DispatchAsync(
                recipients,
                new LocalizedInboxNotificationText(
                    "تم إغلاق الاستبيان",
                    "Survey closed",
                    $"تم إغلاق الاستبيان «{na}».",
                    $"Survey «{ne}» was closed."),
                NotificationRelatedEntityTypes.Survey,
                surveyId,
                null,
                cancellationToken).ConfigureAwait(false);
            return;
        }

        if (to == SurveyStatus.Draft && from == SurveyStatus.Rejected)
        {
            await _notify.DispatchAsync(
                recipients,
                new LocalizedInboxNotificationText(
                    "الاستبيان عاد إلى مسودة",
                    "Survey moved to draft",
                    $"تمت إعادة الاستبيان «{na}» إلى مسودة بعد الرفض.",
                    $"Survey «{ne}» was moved back to draft after rejection."),
                NotificationRelatedEntityTypes.Survey,
                surveyId,
                null,
                cancellationToken).ConfigureAwait(false);
        }
    }

    private static (string TitleAr, string TitleEn) SurveyNamePair(Survey s) =>
    (
        string.IsNullOrWhiteSpace(s.TitleAr) ? s.TitleEn : s.TitleAr,
        string.IsNullOrWhiteSpace(s.TitleEn) ? s.TitleAr : s.TitleEn
    );

    private static string SurveyDisplayLabel(Survey s) =>
        string.IsNullOrWhiteSpace(s.TitleEn) ? s.TitleAr : s.TitleEn;

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

    private static Dictionary<string, (string Ar, string En)> ParseChoiceOptionsMap(string? optionsJson)
    {
        var map = new Dictionary<string, (string Ar, string En)>(StringComparer.Ordinal);
        if (string.IsNullOrWhiteSpace(optionsJson))
            return map;
        try
        {
            using var doc = JsonDocument.Parse(optionsJson);
            if (doc.RootElement.ValueKind != JsonValueKind.Array)
                return map;
            foreach (var el in doc.RootElement.EnumerateArray())
            {
                if (!el.TryGetProperty("value", out var vEl))
                    continue;
                var v = vEl.GetString();
                if (string.IsNullOrEmpty(v))
                    continue;
                var ar = el.TryGetProperty("labelAr", out var la) ? la.GetString() ?? string.Empty : string.Empty;
                var en = el.TryGetProperty("labelEn", out var le) ? le.GetString() ?? string.Empty : string.Empty;
                map[v] = (ar, en);
            }
        }
        catch
        {
            /* ignore malformed options JSON */
        }
        return map;
    }

    private static string? TryDeserializeChoiceValue(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return null;
        var t = json.Trim();
        try
        {
            if (t.StartsWith('['))
                return null;
            return JsonSerializer.Deserialize<string>(json);
        }
        catch
        {
            return t.Trim('"');
        }
    }

    private static IReadOnlyList<string> TryDeserializeChoiceArray(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return Array.Empty<string>();
        var t = json.Trim();
        try
        {
            if (!t.StartsWith('['))
                return Array.Empty<string>();
            var arr = JsonSerializer.Deserialize<string[]>(json);
            return arr ?? Array.Empty<string>();
        }
        catch
        {
            return Array.Empty<string>();
        }
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

    private static string NormalizeAudienceEmail(string email) => email.Trim().ToLowerInvariant();

    private static List<SurveyAudienceMemberInputDto> NormalizeAudiencePayload(
        IReadOnlyList<SurveyAudienceMemberInputDto> members)
    {
        var seenUsers = new HashSet<Guid>();
        var seenEmails = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var list = new List<SurveyAudienceMemberInputDto>();
        foreach (var m in members)
        {
            if (m.UserId is { } uid)
            {
                if (!seenUsers.Add(uid))
                    continue;
                list.Add(new SurveyAudienceMemberInputDto { UserId = uid, Email = null });
            }
            else if (!string.IsNullOrWhiteSpace(m.Email))
            {
                var em = NormalizeAudienceEmail(m.Email);
                if (!seenEmails.Add(em))
                    continue;
                list.Add(new SurveyAudienceMemberInputDto { UserId = null, Email = em });
            }
        }

        return list;
    }

    private async Task<Result> ValidateAudienceMemberInputsAsync(
        IReadOnlyList<SurveyAudienceMemberInputDto>? members,
        CancellationToken cancellationToken)
    {
        if (members is null || members.Count == 0)
            return Result.Ok();

        foreach (var m in NormalizeAudiencePayload(members))
        {
            if (m.UserId is { } uid &&
                !await _db.Users.AnyAsync(u => u.Id == uid && u.IsActive, cancellationToken).ConfigureAwait(false))
            {
                return Result.Fail(
                    "One or more audience users were not found or are inactive.",
                    QuestionnaireErrors.InvalidOperation);
            }
        }

        return Result.Ok();
    }
}
