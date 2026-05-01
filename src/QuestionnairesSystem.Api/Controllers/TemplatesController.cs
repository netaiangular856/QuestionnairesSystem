using System.Diagnostics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QuestionnairesSystem.Api.Extensions;
using QuestionnairesSystem.Application.Features.Identity;
using QuestionnairesSystem.Application.Features.Questionnaires.Templates.DTOs;
using QuestionnairesSystem.Application.Features.Questionnaires.Templates.Interfaces;
using QuestionnairesSystem.Shared.Api;

namespace QuestionnairesSystem.Api.Controllers;

[ApiController]
[Route("api/templates")]
public sealed class TemplatesController : ControllerBase
{
    private readonly ISurveyTemplateService _templates;

    public TemplatesController(ISurveyTemplateService templates) => _templates = templates;

    [HttpPost]
    [Authorize(Policy = PermissionCodes.TemplateManage)]
    public async Task<IActionResult> Create([FromBody] CreateTemplateRequest request, CancellationToken cancellationToken)
    {
        var traceId = Activity.Current?.Id ?? HttpContext.TraceIdentifier;
        var result = await _templates.CreateAsync(request, cancellationToken).ConfigureAwait(false);
        return result.ToApiActionResult(this, traceId);
    }

    [HttpGet]
    [Authorize(Policy = PermissionCodes.TemplateView)]
    public async Task<IActionResult> List([FromQuery] bool includeArchived = false, CancellationToken cancellationToken = default)
    {
        var traceId = Activity.Current?.Id ?? HttpContext.TraceIdentifier;
        var result = await _templates.ListAsync(includeArchived, cancellationToken).ConfigureAwait(false);
        return result.ToApiActionResult(this, traceId);
    }

    [HttpGet("{templateId:guid}")]
    [Authorize(Policy = PermissionCodes.TemplateView)]
    public async Task<IActionResult> Get(Guid templateId, CancellationToken cancellationToken)
    {
        var traceId = Activity.Current?.Id ?? HttpContext.TraceIdentifier;
        var result = await _templates.GetByIdAsync(templateId, cancellationToken).ConfigureAwait(false);
        return result.ToApiActionResult(this, traceId);
    }

    [HttpPut("{templateId:guid}")]
    [Authorize(Policy = PermissionCodes.TemplateManage)]
    public async Task<IActionResult> Update(Guid templateId, [FromBody] UpdateTemplateRequest request, CancellationToken cancellationToken)
    {
        var traceId = Activity.Current?.Id ?? HttpContext.TraceIdentifier;
        var result = await _templates.UpdateAsync(templateId, request, cancellationToken).ConfigureAwait(false);
        return result.ToApiActionResult(this, traceId);
    }

    [HttpDelete("{templateId:guid}")]
    [Authorize(Policy = PermissionCodes.TemplateManage)]
    public async Task<IActionResult> Delete(Guid templateId, CancellationToken cancellationToken)
    {
        var traceId = Activity.Current?.Id ?? HttpContext.TraceIdentifier;
        var result = await _templates.SoftDeleteAsync(templateId, cancellationToken).ConfigureAwait(false);
        return result.ToApiActionResult(this, traceId);
    }

    [HttpPost("{templateId:guid}/use")]
    [Authorize(Policy = PermissionCodes.TemplateManage)]
    public async Task<IActionResult> Use(Guid templateId, CancellationToken cancellationToken)
    {
        var traceId = Activity.Current?.Id ?? HttpContext.TraceIdentifier;
        var result = await _templates.UseAsync(templateId, cancellationToken).ConfigureAwait(false);
        return result.ToApiActionResult(this, traceId);
    }

    [HttpPatch("{templateId:guid}/archive")]
    [Authorize(Policy = PermissionCodes.TemplateManage)]
    public async Task<IActionResult> Archive(Guid templateId, CancellationToken cancellationToken)
    {
        var traceId = Activity.Current?.Id ?? HttpContext.TraceIdentifier;
        var result = await _templates.ArchiveAsync(templateId, cancellationToken).ConfigureAwait(false);
        return result.ToApiActionResult(this, traceId);
    }
}
