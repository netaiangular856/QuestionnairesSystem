using FluentValidation;
using Microsoft.EntityFrameworkCore;
using QuestionnairesSystem.Application.Common;
using QuestionnairesSystem.Application.Features.Questionnaires.Participation.DTOs;
using QuestionnairesSystem.Application.Features.Questionnaires.Participation.Interfaces;
using QuestionnairesSystem.Domain.Enums;
using QuestionnairesSystem.Domain.Participants;
using QuestionnairesSystem.Domain.Responses;
using QuestionnairesSystem.Persistence;
using QuestionnairesSystem.Shared.Api;
using QuestionnairesSystem.Shared.Constants;
using QuestionnairesSystem.Shared.Results;

using QuestionnairesSystem.Shared.Identity;

namespace QuestionnairesSystem.Application.Features.Questionnaires.Participation.Services;

public sealed class ParticipantResponseService : IParticipantResponseService
{
    private readonly QuestionnairesDbContext _db;
    private readonly IValidator<CreateParticipantRequest> _participantValidator;
    private readonly IValidator<CreateResponseRequest> _responseValidator;
    private readonly ICurrentUserService _currentUser;

    public ParticipantResponseService(
        QuestionnairesDbContext db,
        IValidator<CreateParticipantRequest> participantValidator,
        IValidator<CreateResponseRequest> responseValidator,
        ICurrentUserService currentUser)
    {
        _db = db;
        _participantValidator = participantValidator;
        _responseValidator = responseValidator;
        _currentUser = currentUser;
    }

    public async Task<Result<ParticipantDto>> AddParticipantAsync(
        Guid surveyId,
        CreateParticipantRequest request,
        CancellationToken cancellationToken = default)
    {
        var validation = await _participantValidator.ValidateAsync(request, cancellationToken).ConfigureAwait(false);
        if (!validation.IsValid)
            return Result<ParticipantDto>.Fail(validation.ToErrorMessages());

        var exists = await _db.Surveys.AnyAsync(s => s.Id == surveyId, cancellationToken).ConfigureAwait(false);
        if (!exists)
            return Result<ParticipantDto>.Fail("Survey was not found.", QuestionnaireErrors.SurveyNotFound);

        var p = new SurveyParticipant
        {
            SurveyId = surveyId,
            UserId = request.UserId,
            Email = string.IsNullOrWhiteSpace(request.Email) ? null : request.Email.Trim(),
            ExternalReference = string.IsNullOrWhiteSpace(request.ExternalReference) ? null : request.ExternalReference.Trim(),
            Status = ParticipantStatus.Invited,
            InvitedAtUtc = DateTime.UtcNow
        };
        _db.SurveyParticipants.Add(p);
        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        var created = await SelectParticipantDto(_db.SurveyParticipants.AsNoTracking().Where(x => x.Id == p.Id))
            .FirstAsync(cancellationToken)
            .ConfigureAwait(false);
        return Result<ParticipantDto>.Ok(created);
    }

    public async Task<Result<PagedResult<ParticipantDto>>> ListParticipantsAsync(
        Guid surveyId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var exists = await _db.Surveys.AnyAsync(s => s.Id == surveyId, cancellationToken).ConfigureAwait(false);
        if (!exists)
            return Result<PagedResult<ParticipantDto>>.Fail("Survey was not found.", QuestionnaireErrors.SurveyNotFound);

        page = page <= 0 ? PaginationConstants.DefaultPage : page;
        pageSize = pageSize <= 0 ? PaginationConstants.DefaultPageSize : pageSize;
        if (pageSize > PaginationConstants.MaxPageSize) pageSize = PaginationConstants.MaxPageSize;

        var query = SelectParticipantDto(
            _db.SurveyParticipants.AsNoTracking()
                .Where(p => p.SurveyId == surveyId)
                .OrderBy(p => p.InvitedAtUtc));

        var totalCount = await query.CountAsync(cancellationToken).ConfigureAwait(false);
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return Result<PagedResult<ParticipantDto>>.Ok(new PagedResult<ParticipantDto>
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount
        });
    }

    public async Task<Result<ResponseDetailDto>> CreateResponseAsync(
        Guid surveyId,
        CreateResponseRequest request,
        CancellationToken cancellationToken = default)
    {
        var validation = await _responseValidator.ValidateAsync(request, cancellationToken).ConfigureAwait(false);
        if (!validation.IsValid)
            return Result<ResponseDetailDto>.Fail(validation.ToErrorMessages());

        var exists = await _db.Surveys.AnyAsync(s => s.Id == surveyId, cancellationToken).ConfigureAwait(false);
        if (!exists)
            return Result<ResponseDetailDto>.Fail("Survey was not found.", QuestionnaireErrors.SurveyNotFound);

        var userId = request.RespondentUserId ?? _currentUser.UserId;
        var participantId = request.ParticipantId;

        // If no participant linked but we have a user, create a new participant record for this entry
        if (participantId == null)
        {
            var user = userId.HasValue 
                ? await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId, cancellationToken).ConfigureAwait(false)
                : null;

            var p = new SurveyParticipant
            {
                SurveyId = surveyId,
                UserId = userId,
                Email = user?.Email,
                Status = ParticipantStatus.Invited,
                InvitedAtUtc = DateTime.UtcNow
            };
            _db.SurveyParticipants.Add(p);
            await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            participantId = p.Id;
        }

        var r = new SurveyResponse
        {
            SurveyId = surveyId,
            ParticipantId = participantId,
            RespondentUserId = userId,
            Status = ResponseStatus.InProgress,
            StartedAtUtc = DateTime.UtcNow
        };
        _db.SurveyResponses.Add(r);
        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        if (request.Answers is { Count: > 0 })
        {
            foreach (var a in request.Answers)
            {
                _db.QuestionAnswers.Add(new QuestionAnswer
                {
                    ResponseId = r.Id,
                    QuestionId = a.QuestionId,
                    ValueJson = string.IsNullOrWhiteSpace(a.ValueJson) ? "{}" : a.ValueJson
                });
            }

            await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }

        return Result<ResponseDetailDto>.Ok(await LoadResponseDetailAsync(r.Id, cancellationToken).ConfigureAwait(false));
    }

    public async Task<Result<PagedResult<ResponseListItemDto>>> ListResponsesAsync(
        Guid surveyId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var exists = await _db.Surveys.AnyAsync(s => s.Id == surveyId, cancellationToken).ConfigureAwait(false);
        if (!exists)
            return Result<PagedResult<ResponseListItemDto>>.Fail("Survey was not found.", QuestionnaireErrors.SurveyNotFound);

        page = page <= 0 ? PaginationConstants.DefaultPage : page;
        pageSize = pageSize <= 0 ? PaginationConstants.DefaultPageSize : pageSize;
        if (pageSize > PaginationConstants.MaxPageSize) pageSize = PaginationConstants.MaxPageSize;

        var query = _db.SurveyResponses.AsNoTracking()
            .Where(r => r.SurveyId == surveyId)
            .OrderByDescending(r => r.StartedAtUtc);

        var totalCount = await query.CountAsync(cancellationToken).ConfigureAwait(false);
        var list = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(r => new ResponseListItemDto
            {
                Id = r.Id,
                SurveyId = r.SurveyId,
                ParticipantId = r.ParticipantId,
                RespondentDisplayName = r.RespondentUser == null
                    ? null
                    : (r.RespondentUser.NameAr ?? r.RespondentUser.NameEn ?? r.RespondentUser.UserName),
                Status = r.Status,
                StartedAtUtc = r.StartedAtUtc,
                SubmittedAtUtc = r.SubmittedAtUtc
            })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return Result<PagedResult<ResponseListItemDto>>.Ok(new PagedResult<ResponseListItemDto>
        {
            Items = list,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount
        });
    }

    public async Task<Result<ParticipantDetailDto>> GetParticipantDetailAsync(
        Guid surveyId,
        Guid participantId,
        CancellationToken cancellationToken = default)
    {
        var surveyExists = await _db.Surveys.AnyAsync(s => s.Id == surveyId, cancellationToken).ConfigureAwait(false);
        if (!surveyExists)
            return Result<ParticipantDetailDto>.Fail("Survey was not found.", QuestionnaireErrors.SurveyNotFound);

        var participant = await SelectParticipantDto(
                _db.SurveyParticipants.AsNoTracking()
                    .Where(p => p.SurveyId == surveyId && p.Id == participantId))
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);
        if (participant is null)
            return Result<ParticipantDetailDto>.Fail("Participant was not found.", QuestionnaireErrors.ParticipantNotFound);

        var responseId = await _db.SurveyResponses.AsNoTracking()
            .Where(r => r.SurveyId == surveyId && r.ParticipantId == participantId)
            .Select(r => r.Id)
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);

        ResponseDetailDto? response = null;
        if (responseId != Guid.Empty)
            response = await LoadResponseDetailAsync(responseId, cancellationToken).ConfigureAwait(false);

        return Result<ParticipantDetailDto>.Ok(new ParticipantDetailDto
        {
            Participant = participant,
            Response = response
        });
    }

    public async Task<Result<ResponseDetailDto>> GetResponseByIdAsync(Guid responseId, CancellationToken cancellationToken = default)
    {
        var r = await _db.SurveyResponses.AsNoTracking().AnyAsync(x => x.Id == responseId, cancellationToken).ConfigureAwait(false);
        if (!r)
            return Result<ResponseDetailDto>.Fail("Response was not found.", QuestionnaireErrors.ResponseNotFound);

        return Result<ResponseDetailDto>.Ok(await LoadResponseDetailAsync(responseId, cancellationToken).ConfigureAwait(false));
    }

    public async Task<Result<ResponseDetailDto>> SubmitResponseAsync(Guid responseId, CancellationToken cancellationToken = default)
    {
        var r = await _db.SurveyResponses
            .Include(x => x.Participant)
            .FirstOrDefaultAsync(x => x.Id == responseId, cancellationToken).ConfigureAwait(false);
        if (r is null)
            return Result<ResponseDetailDto>.Fail("Response was not found.", QuestionnaireErrors.ResponseNotFound);

        if (r.Status == ResponseStatus.Submitted)
            return Result<ResponseDetailDto>.Fail("Response is already submitted.", QuestionnaireErrors.InvalidOperation);

        r.Status = ResponseStatus.Submitted;
        r.SubmittedAtUtc = DateTime.UtcNow;

        if (r.Participant != null)
        {
            r.Participant.Status = ParticipantStatus.Completed;
            r.Participant.CompletedAtUtc = r.SubmittedAtUtc;
        }

        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Result<ResponseDetailDto>.Ok(await LoadResponseDetailAsync(responseId, cancellationToken).ConfigureAwait(false));
    }

    private async Task<ResponseDetailDto> LoadResponseDetailAsync(Guid responseId, CancellationToken cancellationToken)
    {
        var head = await _db.SurveyResponses.AsNoTracking()
            .Where(x => x.Id == responseId)
            .Select(r => new
            {
                r.Id,
                r.SurveyId,
                r.ParticipantId,
                r.RespondentUserId,
                RespondentDisplayName = r.RespondentUser == null
                    ? null
                    : (r.RespondentUser.NameAr ?? r.RespondentUser.NameEn ?? r.RespondentUser.UserName),
                r.Status,
                r.StartedAtUtc,
                r.SubmittedAtUtc
            })
            .FirstAsync(cancellationToken)
            .ConfigureAwait(false);

        var answers = await _db.QuestionAnswers.AsNoTracking()
            .Where(a => a.ResponseId == responseId)
            .Select(a => new AnswerDto
            {
                QuestionId = a.QuestionId,
                QuestionTitleAr = a.Question.TitleAr,
                QuestionTitleEn = a.Question.TitleEn,
                ValueJson = a.ValueJson
            })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return new ResponseDetailDto
        {
            Id = head.Id,
            SurveyId = head.SurveyId,
            ParticipantId = head.ParticipantId,
            RespondentUserId = head.RespondentUserId,
            RespondentDisplayName = head.RespondentDisplayName,
            Status = head.Status,
            StartedAtUtc = head.StartedAtUtc,
            SubmittedAtUtc = head.SubmittedAtUtc,
            Answers = answers
        };
    }

    private static IQueryable<ParticipantDto> SelectParticipantDto(IQueryable<SurveyParticipant> query) =>
        query.Select(p => new ParticipantDto
        {
            Id = p.Id,
            SurveyId = p.SurveyId,
            UserId = p.UserId,
            UserDisplayName = p.User == null
                ? null
                : (p.User.NameAr ?? p.User.NameEn ?? p.User.UserName),
            Email = p.Email,
            ExternalReference = p.ExternalReference,
            Status = p.Status,
            InvitedAtUtc = p.InvitedAtUtc,
            CompletedAtUtc = p.CompletedAtUtc
        });
}
