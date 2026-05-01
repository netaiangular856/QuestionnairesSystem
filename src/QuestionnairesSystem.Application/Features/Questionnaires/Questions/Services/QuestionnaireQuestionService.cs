using FluentValidation;
using Microsoft.EntityFrameworkCore;
using QuestionnairesSystem.Application.Common;
using QuestionnairesSystem.Application.Features.Questionnaires.Questions.DTOs;
using QuestionnairesSystem.Application.Features.Questionnaires.Questions.Interfaces;
using QuestionnairesSystem.Domain.Enums;
using QuestionnairesSystem.Domain.Surveys;
using QuestionnairesSystem.Persistence;
using QuestionnairesSystem.Shared.Results;

namespace QuestionnairesSystem.Application.Features.Questionnaires.Questions.Services;

public sealed class QuestionnaireQuestionService : IQuestionnaireQuestionService
{
    private readonly QuestionnairesDbContext _db;
    private readonly IValidator<CreateQuestionRequest> _createValidator;
    private readonly IValidator<UpdateQuestionRequest> _updateValidator;
    private readonly IValidator<ReorderQuestionsRequest> _reorderValidator;

    public QuestionnaireQuestionService(
        QuestionnairesDbContext db,
        IValidator<CreateQuestionRequest> createValidator,
        IValidator<UpdateQuestionRequest> updateValidator,
        IValidator<ReorderQuestionsRequest> reorderValidator)
    {
        _db = db;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
        _reorderValidator = reorderValidator;
    }

    public async Task<Result<QuestionDto>> CreateAsync(
        Guid surveyId,
        CreateQuestionRequest request,
        CancellationToken cancellationToken = default)
    {
        var validation = await _createValidator.ValidateAsync(request, cancellationToken).ConfigureAwait(false);
        if (!validation.IsValid)
            return Result<QuestionDto>.Fail(validation.ToErrorMessages());

        var surveyExists = await _db.Surveys.AnyAsync(s => s.Id == surveyId, cancellationToken).ConfigureAwait(false);
        if (!surveyExists)
            return Result<QuestionDto>.Fail("Survey was not found.", QuestionnaireErrors.SurveyNotFound);

        var order = request.DisplayOrder;
        if (order is null or <= 0)
        {
            var max = await _db.Questions.Where(q => q.SurveyId == surveyId).MaxAsync(q => (int?)q.DisplayOrder, cancellationToken)
                .ConfigureAwait(false) ?? 0;
            order = max + 1;
        }

        var q = new Question
        {
            SurveyId = surveyId,
            DisplayOrder = order.Value,
            Type = request.Type,
            TitleAr = request.TitleAr.Trim(),
            TitleEn = request.TitleEn.Trim(),
            HelpTextAr = string.IsNullOrWhiteSpace(request.HelpTextAr) ? null : request.HelpTextAr.Trim(),
            HelpTextEn = string.IsNullOrWhiteSpace(request.HelpTextEn) ? null : request.HelpTextEn.Trim(),
            IsRequired = request.IsRequired,
            OptionsJson = string.IsNullOrWhiteSpace(request.OptionsJson) ? null : request.OptionsJson
        };

        _db.Questions.Add(q);
        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Result<QuestionDto>.Ok(Map(q));
    }

    public async Task<Result<IReadOnlyList<QuestionDto>>> ListBySurveyAsync(Guid surveyId, CancellationToken cancellationToken = default)
    {
        var exists = await _db.Surveys.AnyAsync(s => s.Id == surveyId, cancellationToken).ConfigureAwait(false);
        if (!exists)
            return Result<IReadOnlyList<QuestionDto>>.Fail("Survey was not found.", QuestionnaireErrors.SurveyNotFound);

        var rows = await _db.Questions.AsNoTracking()
            .Where(q => q.SurveyId == surveyId)
            .OrderBy(q => q.DisplayOrder)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        var list = rows.Select(Map).ToList();
        return Result<IReadOnlyList<QuestionDto>>.Ok(list);
    }

    public async Task<Result<QuestionDto>> GetByIdAsync(Guid questionId, CancellationToken cancellationToken = default)
    {
        var q = await _db.Questions.AsNoTracking().FirstOrDefaultAsync(x => x.Id == questionId, cancellationToken)
            .ConfigureAwait(false);
        return q is null
            ? Result<QuestionDto>.Fail("Question was not found.", QuestionnaireErrors.QuestionNotFound)
            : Result<QuestionDto>.Ok(Map(q));
    }

    public async Task<Result<QuestionDto>> UpdateAsync(
        Guid questionId,
        UpdateQuestionRequest request,
        CancellationToken cancellationToken = default)
    {
        var validation = await _updateValidator.ValidateAsync(request, cancellationToken).ConfigureAwait(false);
        if (!validation.IsValid)
            return Result<QuestionDto>.Fail(validation.ToErrorMessages());

        var q = await _db.Questions.FirstOrDefaultAsync(x => x.Id == questionId, cancellationToken).ConfigureAwait(false);
        if (q is null)
            return Result<QuestionDto>.Fail("Question was not found.", QuestionnaireErrors.QuestionNotFound);

        q.TitleAr = request.TitleAr.Trim();
        q.TitleEn = request.TitleEn.Trim();
        q.HelpTextAr = string.IsNullOrWhiteSpace(request.HelpTextAr) ? null : request.HelpTextAr.Trim();
        q.HelpTextEn = string.IsNullOrWhiteSpace(request.HelpTextEn) ? null : request.HelpTextEn.Trim();
        q.IsRequired = request.IsRequired;
        q.OptionsJson = string.IsNullOrWhiteSpace(request.OptionsJson) ? null : request.OptionsJson;
        q.Type = request.Type;
        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Result<QuestionDto>.Ok(Map(q));
    }

    public async Task<Result> SoftDeleteAsync(Guid questionId, CancellationToken cancellationToken = default)
    {
        var q = await _db.Questions.FirstOrDefaultAsync(x => x.Id == questionId, cancellationToken).ConfigureAwait(false);
        if (q is null)
            return Result.Fail("Question was not found.", QuestionnaireErrors.QuestionNotFound);

        q.RecordStatus = RecordStatus.Deleted;
        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Result.Ok();
    }

    public async Task<Result> ReorderAsync(Guid surveyId, ReorderQuestionsRequest request, CancellationToken cancellationToken = default)
    {
        var validation = await _reorderValidator.ValidateAsync(request, cancellationToken).ConfigureAwait(false);
        if (!validation.IsValid)
            return Result.Fail(validation.ToErrorMessages());

        var exists = await _db.Surveys.AnyAsync(s => s.Id == surveyId, cancellationToken).ConfigureAwait(false);
        if (!exists)
            return Result.Fail("Survey was not found.", QuestionnaireErrors.SurveyNotFound);

        var ids = request.QuestionIds.ToList();
        var questions = await _db.Questions.Where(q => q.SurveyId == surveyId && ids.Contains(q.Id)).ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        if (questions.Count != ids.Count)
            return Result.Fail("One or more questions do not belong to this survey.", QuestionnaireErrors.InvalidOperation);

        for (var i = 0; i < ids.Count; i++)
        {
            var q = questions.First(x => x.Id == ids[i]);
            q.DisplayOrder = i + 1;
        }

        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Result.Ok();
    }

    private static QuestionDto Map(Question q) => new()
    {
        Id = q.Id,
        SurveyId = q.SurveyId,
        DisplayOrder = q.DisplayOrder,
        Type = q.Type,
        TitleAr = q.TitleAr,
        TitleEn = q.TitleEn,
        HelpTextAr = q.HelpTextAr,
        HelpTextEn = q.HelpTextEn,
        IsRequired = q.IsRequired,
        OptionsJson = q.OptionsJson
    };
}
