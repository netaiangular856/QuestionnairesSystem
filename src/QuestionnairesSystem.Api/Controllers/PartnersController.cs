using System.Diagnostics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QuestionnairesSystem.Api.Extensions;
using QuestionnairesSystem.Application.Features.Identity;
using QuestionnairesSystem.Application.Features.Partners.DTOs;
using QuestionnairesSystem.Application.Features.Partners.Interfaces;
using QuestionnairesSystem.Shared.Api;

namespace QuestionnairesSystem.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public sealed class PartnersController : ControllerBase
{
    private readonly IPartnerService _partnerService;

    public PartnersController(IPartnerService partnerService)
    {
        _partnerService = partnerService;
    }

    [HttpGet("{id:guid}")]
    [Authorize(PermissionCodes.PartnerView)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var traceId = Activity.Current?.Id ?? HttpContext.TraceIdentifier;
        var result = await _partnerService.GetByIdAsync(id, cancellationToken);
        if (result.IsSuccess)
        {
            return Ok(ApiResponse<PartnerDto>.FromSuccess(result.Value!, traceId));
        }
        return result.ToApiActionResult(this, traceId);
    }

    [HttpGet]
    [Authorize(PermissionCodes.PartnerView)]
    public async Task<IActionResult> GetPagedList([FromQuery] PartnerFilterRequest request, CancellationToken cancellationToken)
    {
        var traceId = Activity.Current?.Id ?? HttpContext.TraceIdentifier;
        var result = await _partnerService.GetPagedListAsync(request, cancellationToken);
        if (result.IsSuccess)
        {
            return Ok(ApiResponse<PagedResult<PartnerListItemDto>>.FromSuccess(result.Value!, traceId));
        }
        return result.ToApiActionResult(this, traceId);
    }

    [HttpPost]
    [Authorize(PermissionCodes.PartnerManage)]
    public async Task<IActionResult> Create(CreatePartnerRequest request, CancellationToken cancellationToken)
    {
        var traceId = Activity.Current?.Id ?? HttpContext.TraceIdentifier;
        var result = await _partnerService.CreateAsync(request, cancellationToken);
        if (result.IsSuccess)
        {
            return CreatedAtAction(nameof(GetById), new { id = result.Value!.Id }, 
                ApiResponse<PartnerDto>.FromSuccess(result.Value, traceId));
        }
        return result.ToApiActionResult(this, traceId);
    }

    [HttpPut("{id:guid}")]
    [Authorize(PermissionCodes.PartnerManage)]
    public async Task<IActionResult> Update(Guid id, UpdatePartnerRequest request, CancellationToken cancellationToken)
    {
        var traceId = Activity.Current?.Id ?? HttpContext.TraceIdentifier;
        var result = await _partnerService.UpdateAsync(id, request, cancellationToken);
        if (result.IsSuccess)
        {
            return Ok(ApiResponse<PartnerDto>.FromSuccess(result.Value!, traceId));
        }
        return result.ToApiActionResult(this, traceId);
    }

    [HttpDelete("{id:guid}")]
    [Authorize(PermissionCodes.PartnerManage)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var traceId = Activity.Current?.Id ?? HttpContext.TraceIdentifier;
        var result = await _partnerService.DeleteAsync(id, cancellationToken);
        if (result.IsSuccess)
        {
            return NoContent();
        }
        return result.ToApiActionResult(this, traceId);
    }
}
