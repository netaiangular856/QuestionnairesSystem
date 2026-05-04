using FluentValidation;
using Microsoft.EntityFrameworkCore;
using QuestionnairesSystem.Application.Common;
using QuestionnairesSystem.Application.Features.Notifications;
using QuestionnairesSystem.Application.Features.Notifications.DTOs;
using QuestionnairesSystem.Application.Features.Notifications.Interfaces;
using QuestionnairesSystem.Application.Features.Questionnaires.Recommendations.DTOs;
using QuestionnairesSystem.Application.Features.Questionnaires.Recommendations.Interfaces;
using QuestionnairesSystem.Domain.Enums;
using QuestionnairesSystem.Domain.Recommendations;
using QuestionnairesSystem.Persistence;
using QuestionnairesSystem.Shared.Api;
using QuestionnairesSystem.Shared.Identity;
using QuestionnairesSystem.Shared.Results;

namespace QuestionnairesSystem.Application.Features.Questionnaires.Recommendations.Services;

public sealed class RecommendationCrudService : IRecommendationCrudService
{
    private readonly QuestionnairesDbContext _db;
    private readonly IValidator<CreateRecommendationRequest> _createValidator;
    private readonly IValidator<UpdateRecommendationRequest> _updateValidator;
    private readonly ICurrentUserService _currentUser;
    private readonly IInboxNotificationDispatchService _notify;

    public RecommendationCrudService(
        QuestionnairesDbContext db,
        IValidator<CreateRecommendationRequest> createValidator,
        IValidator<UpdateRecommendationRequest> updateValidator,
        ICurrentUserService currentUser,
        IInboxNotificationDispatchService notify)
    {
        _db = db;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
        _currentUser = currentUser;
        _notify = notify;
    }

    public async Task<Result<RecommendationDto>> CreateAsync(CreateRecommendationRequest request, CancellationToken cancellationToken = default)
    {
        var validation = await _createValidator.ValidateAsync(request, cancellationToken).ConfigureAwait(false);
        if (!validation.IsValid)
            return Result<RecommendationDto>.Fail(validation.ToErrorMessages());

        if (request.SurveyId is { } sid && !await _db.Surveys.AnyAsync(s => s.Id == sid, cancellationToken).ConfigureAwait(false))
            return Result<RecommendationDto>.Fail("Survey was not found.", QuestionnaireErrors.SurveyNotFound);

        var r = new Recommendation
        {
            SurveyId = request.SurveyId,
            TitleAr = request.TitleAr.Trim(),
            TitleEn = request.TitleEn.Trim(),
            DescriptionAr = string.IsNullOrWhiteSpace(request.DescriptionAr) ? null : request.DescriptionAr.Trim(),
            DescriptionEn = string.IsNullOrWhiteSpace(request.DescriptionEn) ? null : request.DescriptionEn.Trim(),
            Priority = request.Priority,
            Status = RecommendationStatus.Draft,
            AssignedToUserId = request.AssignedToUserId,
            DueDateUtc = request.DueDateUtc
        };
        _db.Recommendations.Add(r);
        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        var created = await SelectRecommendationDto(_db.Recommendations.AsNoTracking().Where(x => x.Id == r.Id))
            .FirstAsync(cancellationToken)
            .ConfigureAwait(false);

        var (na, ne) = RecommendationNamePair(r);
        var createRecipients = new HashSet<Guid>();
        if (_currentUser.UserId is { } actorCr)
            createRecipients.Add(actorCr);
        if (r.AssignedToUserId is { } assignCr)
            createRecipients.Add(assignCr);
        await _notify.DispatchAsync(
            createRecipients,
            new LocalizedInboxNotificationText(
                "تم إنشاء توصية",
                "Recommendation created",
                $"تم إنشاء التوصية «{na}».",
                $"Recommendation «{ne}» was created."),
            NotificationRelatedEntityTypes.Recommendation,
            r.Id,
            null,
            cancellationToken).ConfigureAwait(false);

        return Result<RecommendationDto>.Ok(created);
    }

    public async Task<Result<PagedResult<RecommendationDto>>> ListPagedAsync(int page, int pageSize, CancellationToken cancellationToken = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);
        var baseQuery = _db.Recommendations.AsNoTracking();
        var totalCount = await baseQuery.CountAsync(cancellationToken).ConfigureAwait(false);
        var items = await SelectRecommendationDto(
                baseQuery
                    .OrderByDescending(r => r.Priority)
                    .ThenBy(r => r.TitleEn)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        var paged = new PagedResult<RecommendationDto>
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount,
        };
        return Result<PagedResult<RecommendationDto>>.Ok(paged);
    }

    public async Task<Result<RecommendationDto>> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var dto = await SelectRecommendationDto(_db.Recommendations.AsNoTracking().Where(x => x.Id == id))
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);
        return dto is null
            ? Result<RecommendationDto>.Fail("Recommendation was not found.", QuestionnaireErrors.RecommendationNotFound)
            : Result<RecommendationDto>.Ok(dto);
    }

    public async Task<Result<RecommendationDto>> UpdateAsync(
        Guid id,
        UpdateRecommendationRequest request,
        CancellationToken cancellationToken = default)
    {
        var validation = await _updateValidator.ValidateAsync(request, cancellationToken).ConfigureAwait(false);
        if (!validation.IsValid)
            return Result<RecommendationDto>.Fail(validation.ToErrorMessages());

        var r = await _db.Recommendations.FirstOrDefaultAsync(x => x.Id == id, cancellationToken).ConfigureAwait(false);
        if (r is null)
            return Result<RecommendationDto>.Fail("Recommendation was not found.", QuestionnaireErrors.RecommendationNotFound);

        var previousAssignee = r.AssignedToUserId;

        if (request.SurveyId is { } sid && !await _db.Surveys.AnyAsync(s => s.Id == sid, cancellationToken).ConfigureAwait(false))
            return Result<RecommendationDto>.Fail("Survey was not found.", QuestionnaireErrors.SurveyNotFound);

        r.SurveyId = request.SurveyId;
        r.TitleAr = request.TitleAr.Trim();
        r.TitleEn = request.TitleEn.Trim();
        r.DescriptionAr = string.IsNullOrWhiteSpace(request.DescriptionAr) ? null : request.DescriptionAr.Trim();
        r.DescriptionEn = string.IsNullOrWhiteSpace(request.DescriptionEn) ? null : request.DescriptionEn.Trim();
        r.Priority = request.Priority;
        r.Status = request.Status;
        r.AssignedToUserId = request.AssignedToUserId;
        r.DueDateUtc = request.DueDateUtc;
        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        var updated = await SelectRecommendationDto(_db.Recommendations.AsNoTracking().Where(x => x.Id == r.Id))
            .FirstAsync(cancellationToken)
            .ConfigureAwait(false);

        var (naUpd, neUpd) = RecommendationNamePair(r);
        var updRecipients = new HashSet<Guid>();
        if (_currentUser.UserId is { } actorUpd)
            updRecipients.Add(actorUpd);
        if (r.AssignedToUserId is { } newAsg)
            updRecipients.Add(newAsg);
        if (previousAssignee is { } oldAsg && oldAsg != r.AssignedToUserId)
            updRecipients.Add(oldAsg);
        await _notify.DispatchAsync(
            updRecipients,
            new LocalizedInboxNotificationText(
                "تم تحديث توصية",
                "Recommendation updated",
                $"تم تحديث التوصية «{naUpd}».",
                $"Recommendation «{neUpd}» was updated."),
            NotificationRelatedEntityTypes.Recommendation,
            r.Id,
            null,
            cancellationToken).ConfigureAwait(false);

        return Result<RecommendationDto>.Ok(updated);
    }

    public async Task<Result> SoftDeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var r = await _db.Recommendations.FirstOrDefaultAsync(x => x.Id == id, cancellationToken).ConfigureAwait(false);
        if (r is null)
            return Result.Fail("Recommendation was not found.", QuestionnaireErrors.RecommendationNotFound);

        var (naDel, neDel) = RecommendationNamePair(r);
        var delAssignee = r.AssignedToUserId;
        r.RecordStatus = RecordStatus.Deleted;
        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        var delRecipients = new HashSet<Guid>();
        if (_currentUser.UserId is { } actorDel)
            delRecipients.Add(actorDel);
        if (delAssignee is { } asgDel)
            delRecipients.Add(asgDel);
        await _notify.DispatchAsync(
            delRecipients,
            new LocalizedInboxNotificationText(
                "تم حذف توصية",
                "Recommendation deleted",
                $"تم حذف التوصية «{naDel}».",
                $"Recommendation «{neDel}» was deleted."),
            NotificationRelatedEntityTypes.Recommendation,
            r.Id,
            null,
            cancellationToken).ConfigureAwait(false);

        return Result.Ok();
    }

    private static (string Ar, string En) RecommendationNamePair(Recommendation r) =>
    (
        string.IsNullOrWhiteSpace(r.TitleAr) ? r.TitleEn : r.TitleAr,
        string.IsNullOrWhiteSpace(r.TitleEn) ? r.TitleAr : r.TitleEn
    );

    private static IQueryable<RecommendationDto> SelectRecommendationDto(IQueryable<Recommendation> query) =>
        query.Select(r => new RecommendationDto
        {
            Id = r.Id,
            SurveyId = r.SurveyId,
            TitleAr = r.TitleAr,
            TitleEn = r.TitleEn,
            DescriptionAr = r.DescriptionAr,
            DescriptionEn = r.DescriptionEn,
            Priority = r.Priority,
            Status = r.Status,
            AssignedToUserId = r.AssignedToUserId,
            AssignedToDisplayName = r.AssignedToUser == null
                ? null
                : (r.AssignedToUser.NameAr ?? r.AssignedToUser.NameEn ?? r.AssignedToUser.UserName),
            DueDateUtc = r.DueDateUtc
        });
}
