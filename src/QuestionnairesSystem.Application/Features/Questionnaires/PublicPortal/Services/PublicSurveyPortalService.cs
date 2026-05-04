using Microsoft.EntityFrameworkCore;
using QuestionnairesSystem.Application.Common;
using QuestionnairesSystem.Application.Features.Questionnaires.Participation.DTOs;
using QuestionnairesSystem.Application.Features.Questionnaires.Participation.Interfaces;
using QuestionnairesSystem.Application.Features.Questionnaires.PublicPortal.DTOs;
using QuestionnairesSystem.Application.Features.Questionnaires.PublicPortal.Interfaces;
using QuestionnairesSystem.Application.Features.Questionnaires.Questions.DTOs;
using QuestionnairesSystem.Application.Features.Questionnaires.Questions.Interfaces;
using QuestionnairesSystem.Domain.Enums;
using QuestionnairesSystem.Persistence;
using QuestionnairesSystem.Shared.Api;
using QuestionnairesSystem.Shared.Constants;
using QuestionnairesSystem.Shared.Results;

namespace QuestionnairesSystem.Application.Features.Questionnaires.PublicPortal.Services;

public sealed class PublicSurveyPortalService : IPublicSurveyPortalService
{
    private readonly QuestionnairesDbContext _db;
    private readonly IParticipantResponseService _responses;
    private readonly IQuestionnaireQuestionService _questions;

    public PublicSurveyPortalService(
        QuestionnairesDbContext db,
        IParticipantResponseService responses,
        IQuestionnaireQuestionService questions)
    {
        _db = db;
        _responses = responses;
        _questions = questions;
    }

    public async Task<Result<PagedResult<PublicSurveyListItemDto>>> GetCatalogAsync(
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        page = page <= 0 ? PaginationConstants.DefaultPage : page;
        pageSize = pageSize <= 0 ? PaginationConstants.DefaultPageSize : pageSize;
        if (pageSize > PaginationConstants.MaxPageSize)
            pageSize = PaginationConstants.MaxPageSize;

        var now = DateTime.UtcNow;
        var query = _db.Surveys.AsNoTracking()
            .Where(s => s.Status == SurveyStatus.Published)
            .Where(s => s.ClosedAtUtc == null)
            .Where(s => s.ShowOnPublicPortal)
            .Where(s => s.Code != null && s.Code != "")
            .Where(s =>
                s.AudienceScope == SurveyAudienceScope.Everyone ||
                s.AudienceScope == SurveyAudienceScope.Guest)
            .Where(s => s.OpensAtUtc == null || s.OpensAtUtc <= now)
            .Where(s => s.ClosesAtUtc == null || s.ClosesAtUtc >= now);

        var totalCount = await query.CountAsync(cancellationToken).ConfigureAwait(false);
        var items = await query
            .OrderByDescending(s => s.PublishedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(s => new PublicSurveyListItemDto
            {
                Id = s.Id,
                TitleAr = s.TitleAr,
                TitleEn = s.TitleEn,
                Code = s.Code!,
                DescriptionAr = s.DescriptionAr,
                DescriptionEn = s.DescriptionEn,
                ClosesAtUtc = s.ClosesAtUtc
            })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return Result<PagedResult<PublicSurveyListItemDto>>.Ok(new PagedResult<PublicSurveyListItemDto>
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount
        });
    }

    public async Task<Result<PublicSurveyPageDto>> GetSurveyByCodeAsync(string code, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(code))
            return Result<PublicSurveyPageDto>.Fail("Survey code is required.", QuestionnaireErrors.SurveyNotFound);

        var norm = code.Trim();
        var now = DateTime.UtcNow;

        var row = await _db.Surveys.AsNoTracking()
            .Where(s => s.Code != null && s.Code.ToLower() == norm.ToLower())
            .Where(s => s.Status == SurveyStatus.Published)
            .Where(s => s.ClosedAtUtc == null)
            .Where(s =>
                s.AudienceScope == SurveyAudienceScope.Everyone ||
                s.AudienceScope == SurveyAudienceScope.Guest)
            .Where(s => s.OpensAtUtc == null || s.OpensAtUtc <= now)
            .Where(s => s.ClosesAtUtc == null || s.ClosesAtUtc >= now)
            .Select(s => new PublicSurveyPageDto
            {
                Id = s.Id,
                TitleAr = s.TitleAr,
                TitleEn = s.TitleEn,
                DescriptionAr = s.DescriptionAr,
                DescriptionEn = s.DescriptionEn,
                Code = s.Code!,
                OpensAtUtc = s.OpensAtUtc,
                ClosesAtUtc = s.ClosesAtUtc,
                PublicArticleEnabled = s.PublicArticleEnabled,
                PublicArticleTitleAr = s.PublicArticleTitleAr,
                PublicArticleTitleEn = s.PublicArticleTitleEn,
                PublicArticleBodyAr = s.PublicArticleBodyAr,
                PublicArticleBodyEn = s.PublicArticleBodyEn
            })
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);

        return row is null
            ? Result<PublicSurveyPageDto>.Fail("Survey was not found.", QuestionnaireErrors.SurveyNotFound)
            : Result<PublicSurveyPageDto>.Ok(row);
    }

    public async Task<Result<IReadOnlyList<QuestionDto>>> ListQuestionsByCodeAsync(
        string code,
        CancellationToken cancellationToken = default)
    {
        var page = await GetSurveyByCodeAsync(code, cancellationToken).ConfigureAwait(false);
        if (!page.IsSuccess || page.Value is null)
            return Result<IReadOnlyList<QuestionDto>>.Fail(page.Errors, page.FailureCode);

        return await _questions.ListBySurveyAsync(page.Value.Id, cancellationToken).ConfigureAwait(false);
    }

    public Task<Result<ResponseDetailDto>> CreateResponseByCodeAsync(
        string code,
        CreateResponseRequest request,
        CancellationToken cancellationToken = default) =>
        _responses.CreatePublicResponseAsync(code, request, cancellationToken);

    public Task<Result<ResponseDetailDto>> SubmitResponseByCodeAsync(
        string code,
        Guid responseId,
        CancellationToken cancellationToken = default) =>
        _responses.SubmitPublicResponseAsync(code, responseId, cancellationToken);
}
