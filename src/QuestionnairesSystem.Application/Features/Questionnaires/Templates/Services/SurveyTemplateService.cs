using FluentValidation;
using Microsoft.EntityFrameworkCore;
using QuestionnairesSystem.Application.Common;
using QuestionnairesSystem.Application.Features.Questionnaires.Surveys.DTOs;
using QuestionnairesSystem.Application.Features.Questionnaires.Surveys.Interfaces;
using QuestionnairesSystem.Application.Features.Questionnaires.Templates.DTOs;
using QuestionnairesSystem.Application.Features.Questionnaires.Templates.Interfaces;
using QuestionnairesSystem.Application.Features.Questionnaires.Templates;
using QuestionnairesSystem.Domain.Enums;
using QuestionnairesSystem.Domain.Templates;
using QuestionnairesSystem.Persistence;
using QuestionnairesSystem.Shared.Results;

namespace QuestionnairesSystem.Application.Features.Questionnaires.Templates.Services;

public sealed class SurveyTemplateService : ISurveyTemplateService
{
    private readonly QuestionnairesDbContext _db;
    private readonly ISurveyService _surveys;
    private readonly IValidator<CreateTemplateRequest> _createValidator;
    private readonly IValidator<UpdateTemplateRequest> _updateValidator;

    public SurveyTemplateService(
        QuestionnairesDbContext db,
        ISurveyService surveys,
        IValidator<CreateTemplateRequest> createValidator,
        IValidator<UpdateTemplateRequest> updateValidator)
    {
        _db = db;
        _surveys = surveys;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
    }

    public async Task<Result<TemplateDetailDto>> CreateAsync(CreateTemplateRequest request, CancellationToken cancellationToken = default)
    {
        var validation = await _createValidator.ValidateAsync(request, cancellationToken).ConfigureAwait(false);
        if (!validation.IsValid)
            return Result<TemplateDetailDto>.Fail(validation.ToErrorMessages());

        var structureJson = TemplateStructureJson.FromQuestions(request.Questions);

        var t = new SurveyTemplate
        {
            NameAr = request.NameAr.Trim(),
            NameEn = request.NameEn.Trim(),
            DescriptionAr = string.IsNullOrWhiteSpace(request.DescriptionAr) ? null : request.DescriptionAr.Trim(),
            DescriptionEn = string.IsNullOrWhiteSpace(request.DescriptionEn) ? null : request.DescriptionEn.Trim(),
            StructureJson = structureJson
        };
        _db.SurveyTemplates.Add(t);
        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Result<TemplateDetailDto>.Ok(Map(t));
    }

    public async Task<Result<IReadOnlyList<TemplateListItemDto>>> ListAsync(bool includeArchived, CancellationToken cancellationToken = default)
    {
        var q = _db.SurveyTemplates.AsNoTracking();
        if (!includeArchived)
            q = q.Where(t => !t.IsArchived);

        var rows = await q
            .OrderBy(t => t.NameEn)
            .Select(t => new
            {
                t.Id,
                t.NameAr,
                t.NameEn,
                t.IsArchived,
                t.UsageCount,
                t.StructureJson
            })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var list = rows
            .Select(t => new TemplateListItemDto
            {
                Id = t.Id,
                NameAr = t.NameAr,
                NameEn = t.NameEn,
                IsArchived = t.IsArchived,
                UsageCount = t.UsageCount,
                QuestionCount = TemplateStructureJson.CountQuestions(t.StructureJson)
            })
            .ToList();
        return Result<IReadOnlyList<TemplateListItemDto>>.Ok(list);
    }

    public async Task<Result<TemplateDetailDto>> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var t = await _db.SurveyTemplates.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
            .ConfigureAwait(false);
        return t is null
            ? Result<TemplateDetailDto>.Fail("Template was not found.", QuestionnaireErrors.TemplateNotFound)
            : Result<TemplateDetailDto>.Ok(Map(t));
    }

    public async Task<Result<TemplateDetailDto>> UpdateAsync(Guid id, UpdateTemplateRequest request, CancellationToken cancellationToken = default)
    {
        var validation = await _updateValidator.ValidateAsync(request, cancellationToken).ConfigureAwait(false);
        if (!validation.IsValid)
            return Result<TemplateDetailDto>.Fail(validation.ToErrorMessages());

        var t = await _db.SurveyTemplates.FirstOrDefaultAsync(x => x.Id == id, cancellationToken).ConfigureAwait(false);
        if (t is null)
            return Result<TemplateDetailDto>.Fail("Template was not found.", QuestionnaireErrors.TemplateNotFound);

        var structureJson = TemplateStructureJson.FromQuestions(request.Questions);

        t.NameAr = request.NameAr.Trim();
        t.NameEn = request.NameEn.Trim();
        t.DescriptionAr = string.IsNullOrWhiteSpace(request.DescriptionAr) ? null : request.DescriptionAr.Trim();
        t.DescriptionEn = string.IsNullOrWhiteSpace(request.DescriptionEn) ? null : request.DescriptionEn.Trim();
        t.StructureJson = structureJson;
        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Result<TemplateDetailDto>.Ok(Map(t));
    }

    public async Task<Result> SoftDeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var t = await _db.SurveyTemplates.FirstOrDefaultAsync(x => x.Id == id, cancellationToken).ConfigureAwait(false);
        if (t is null)
            return Result.Fail("Template was not found.", QuestionnaireErrors.TemplateNotFound);

        t.RecordStatus = RecordStatus.Deleted;
        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Result.Ok();
    }

    public async Task<Result<UseTemplateResultDto>> UseAsync(Guid templateId, CancellationToken cancellationToken = default)
    {
        var t = await _db.SurveyTemplates.AsNoTracking().FirstOrDefaultAsync(x => x.Id == templateId, cancellationToken)
            .ConfigureAwait(false);
        if (t is null)
            return Result<UseTemplateResultDto>.Fail("Template was not found.", QuestionnaireErrors.TemplateNotFound);

        if (t.IsArchived)
            return Result<UseTemplateResultDto>.Fail("Archived template cannot be used.", QuestionnaireErrors.InvalidOperation);

        var created = await _surveys.CreateAsync(
                new CreateSurveyRequest
                {
                    TitleAr = t.NameAr,
                    TitleEn = t.NameEn,
                    DescriptionAr = t.DescriptionAr,
                    DescriptionEn = t.DescriptionEn,
                    TemplateId = templateId,
                    AudienceScope = SurveyAudienceScope.AllOrganizationMembers
                },
                cancellationToken)
            .ConfigureAwait(false);
        if (!created.IsSuccess)
            return Result<UseTemplateResultDto>.Fail(created.Errors, created.FailureCode);

        return Result<UseTemplateResultDto>.Ok(new UseTemplateResultDto { SurveyId = created.Value!.Id });
    }

    public async Task<Result<TemplateDetailDto>> ArchiveAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var t = await _db.SurveyTemplates.FirstOrDefaultAsync(x => x.Id == id, cancellationToken).ConfigureAwait(false);
        if (t is null)
            return Result<TemplateDetailDto>.Fail("Template was not found.", QuestionnaireErrors.TemplateNotFound);

        t.IsArchived = true;
        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Result<TemplateDetailDto>.Ok(Map(t));
    }

    private static TemplateDetailDto Map(SurveyTemplate t)
    {
        var questions = TemplateStructureJson.ParseQuestions(t.StructureJson);
        return new TemplateDetailDto
        {
            Id = t.Id,
            NameAr = t.NameAr,
            NameEn = t.NameEn,
            DescriptionAr = t.DescriptionAr,
            DescriptionEn = t.DescriptionEn,
            Questions = questions,
            IsArchived = t.IsArchived,
            UsageCount = t.UsageCount,
            QuestionCount = TemplateStructureJson.CountQuestions(t.StructureJson)
        };
    }
}
