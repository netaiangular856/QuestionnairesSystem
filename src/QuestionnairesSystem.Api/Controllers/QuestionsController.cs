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
[Route("api/questions")]
public sealed class QuestionsController : ControllerBase
{
    private readonly IQuestionnaireQuestionService _questions;

    public QuestionsController(IQuestionnaireQuestionService questions) => _questions = questions;

    [HttpGet("{questionId:guid}")]
    [Authorize(Policy = PermissionCodes.QuestionView)]
    public async Task<IActionResult> Get(Guid questionId, CancellationToken cancellationToken)
    {
        var traceId = Activity.Current?.Id ?? HttpContext.TraceIdentifier;
        var result = await _questions.GetByIdAsync(questionId, cancellationToken).ConfigureAwait(false);
        return result.ToApiActionResult(this, traceId);
    }

    [HttpPut("{questionId:guid}")]
    [Authorize(Policy = PermissionCodes.QuestionManage)]
    public async Task<IActionResult> Update(Guid questionId, [FromBody] UpdateQuestionRequest request, CancellationToken cancellationToken)
    {
        var traceId = Activity.Current?.Id ?? HttpContext.TraceIdentifier;
        var result = await _questions.UpdateAsync(questionId, request, cancellationToken).ConfigureAwait(false);
        return result.ToApiActionResult(this, traceId);
    }

    [HttpDelete("{questionId:guid}")]
    [Authorize(Policy = PermissionCodes.QuestionManage)]
    public async Task<IActionResult> Delete(Guid questionId, CancellationToken cancellationToken)
    {
        var traceId = Activity.Current?.Id ?? HttpContext.TraceIdentifier;
        var result = await _questions.SoftDeleteAsync(questionId, cancellationToken).ConfigureAwait(false);
        return result.ToApiActionResult(this, traceId);
    }
}
