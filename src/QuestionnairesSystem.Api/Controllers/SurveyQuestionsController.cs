using System.Diagnostics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QuestionnairesSystem.Api.Extensions;
using QuestionnairesSystem.Application.Features.Identity;
using QuestionnairesSystem.Application.Features.Questionnaires.Questions.DTOs;
using QuestionnairesSystem.Application.Features.Questionnaires.Questions.Interfaces;
using QuestionnairesSystem.Shared.Api;

namespace QuestionnairesSystem.Api.Controllers;

[ApiController]
[Route("api/surveys/{surveyId:guid}/questions")]
public sealed class SurveyQuestionsController : ControllerBase
{
    private readonly IQuestionnaireQuestionService _questions;

    public SurveyQuestionsController(IQuestionnaireQuestionService questions) => _questions = questions;

    [HttpPost]
    [Authorize(Policy = PermissionCodes.QuestionManage)]
    public async Task<IActionResult> Create(Guid surveyId, [FromBody] CreateQuestionRequest request, CancellationToken cancellationToken)
    {
        var traceId = Activity.Current?.Id ?? HttpContext.TraceIdentifier;
        var result = await _questions.CreateAsync(surveyId, request, cancellationToken).ConfigureAwait(false);
        return result.ToApiActionResult(this, traceId);
    }

    [HttpGet]
    [Authorize(Policy = PermissionCodes.QuestionView)]
    public async Task<IActionResult> List(Guid surveyId, CancellationToken cancellationToken)
    {
        var traceId = Activity.Current?.Id ?? HttpContext.TraceIdentifier;
        var result = await _questions.ListBySurveyAsync(surveyId, cancellationToken).ConfigureAwait(false);
        return result.ToApiActionResult(this, traceId);
    }

    [HttpPatch("reorder")]
    [Authorize(Policy = PermissionCodes.QuestionManage)]
    public async Task<IActionResult> Reorder(Guid surveyId, [FromBody] ReorderQuestionsRequest request, CancellationToken cancellationToken)
    {
        var traceId = Activity.Current?.Id ?? HttpContext.TraceIdentifier;
        var result = await _questions.ReorderAsync(surveyId, request, cancellationToken).ConfigureAwait(false);
        return result.ToApiActionResult(this, traceId);
    }
}
