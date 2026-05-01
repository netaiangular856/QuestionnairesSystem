using System.Diagnostics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QuestionnairesSystem.Api.Extensions;
using QuestionnairesSystem.Application.Features.Identity;
using QuestionnairesSystem.Application.Features.Questionnaires.Recommendations.DTOs;
using QuestionnairesSystem.Application.Features.Questionnaires.Recommendations.Interfaces;
using QuestionnairesSystem.Shared.Api;

namespace QuestionnairesSystem.Api.Controllers;

[ApiController]
[Route("api/recommendations")]
public sealed class RecommendationsController : ControllerBase
{
    private readonly IRecommendationCrudService _svc;

    public RecommendationsController(IRecommendationCrudService svc) => _svc = svc;

    [HttpPost]
    [Authorize(Policy = PermissionCodes.RecommendationManage)]
    public async Task<IActionResult> Create([FromBody] CreateRecommendationRequest request, CancellationToken cancellationToken)
    {
        var traceId = Activity.Current?.Id ?? HttpContext.TraceIdentifier;
        var result = await _svc.CreateAsync(request, cancellationToken).ConfigureAwait(false);
        return result.ToApiActionResult(this, traceId);
    }

    [HttpGet]
    [Authorize(Policy = PermissionCodes.RecommendationView)]
    public async Task<IActionResult> List(CancellationToken cancellationToken)
    {
        var traceId = Activity.Current?.Id ?? HttpContext.TraceIdentifier;
        var result = await _svc.ListAsync(cancellationToken).ConfigureAwait(false);
        return result.ToApiActionResult(this, traceId);
    }

    [HttpGet("{recommendationId:guid}")]
    [Authorize(Policy = PermissionCodes.RecommendationView)]
    public async Task<IActionResult> Get(Guid recommendationId, CancellationToken cancellationToken)
    {
        var traceId = Activity.Current?.Id ?? HttpContext.TraceIdentifier;
        var result = await _svc.GetByIdAsync(recommendationId, cancellationToken).ConfigureAwait(false);
        return result.ToApiActionResult(this, traceId);
    }

    [HttpPut("{recommendationId:guid}")]
    [Authorize(Policy = PermissionCodes.RecommendationManage)]
    public async Task<IActionResult> Update(Guid recommendationId, [FromBody] UpdateRecommendationRequest request, CancellationToken cancellationToken)
    {
        var traceId = Activity.Current?.Id ?? HttpContext.TraceIdentifier;
        var result = await _svc.UpdateAsync(recommendationId, request, cancellationToken).ConfigureAwait(false);
        return result.ToApiActionResult(this, traceId);
    }

    [HttpDelete("{recommendationId:guid}")]
    [Authorize(Policy = PermissionCodes.RecommendationManage)]
    public async Task<IActionResult> Delete(Guid recommendationId, CancellationToken cancellationToken)
    {
        var traceId = Activity.Current?.Id ?? HttpContext.TraceIdentifier;
        var result = await _svc.SoftDeleteAsync(recommendationId, cancellationToken).ConfigureAwait(false);
        return result.ToApiActionResult(this, traceId);
    }
}
