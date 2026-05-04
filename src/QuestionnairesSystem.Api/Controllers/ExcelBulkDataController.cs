using System.Diagnostics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QuestionnairesSystem.Api.Extensions;
using QuestionnairesSystem.Application.Features.DataBulk;
using QuestionnairesSystem.Application.Features.Identity;

namespace QuestionnairesSystem.Api.Controllers;

[Authorize(PermissionCodes.DataBulkImport)]
[ApiController]
[Route("api/excel-bulk")]
public sealed class ExcelBulkDataController : ControllerBase
{
    private readonly IExcelBulkDataService _excelBulkDataService;

    public ExcelBulkDataController(IExcelBulkDataService excelBulkDataService)
    {
        _excelBulkDataService = excelBulkDataService;
    }

    /// <summary>
    /// scope: all | departments | employees | partners | users | templates (قوالب = SurveyTemplates+SurveyTemplateQuestions) | surveys (Surveys+SurveyQuestions+SurveyResponses).
    /// samples: false = رؤوس أعمدة فقط، true = صفوف أمثلة.
    /// </summary>
    [HttpGet("template")]
    public async Task<IActionResult> DownloadTemplate(
        [FromQuery] string scope = "all",
        [FromQuery] bool samples = false,
        CancellationToken cancellationToken = default)
    {
        if (!TryParseScope(scope, out var sc))
        {
            return BadRequest(Shared.Api.ApiResponse<object>.FromFailure(
                new[] { "Invalid scope. Use: all, departments, employees, partners, users, templates, surveys." },
                HttpContext.TraceIdentifier));
        }

        var bytes = await _excelBulkDataService.GetTemplateAsync(sc, samples, cancellationToken).ConfigureAwait(false);
        var fileName = BuildTemplateFileName(sc, samples);
        const string contentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
        return File(bytes, contentType, fileName);
    }

    private static bool TryParseScope(string? raw, out ExcelTemplateScope scope)
    {
        scope = ExcelTemplateScope.All;
        if (string.IsNullOrWhiteSpace(raw))
        {
            return true;
        }

        return Enum.TryParse(raw.Trim(), ignoreCase: true, out scope);
    }

    private static string BuildTemplateFileName(ExcelTemplateScope sc, bool samples)
    {
        var tag = sc.ToString().ToLowerInvariant();
        var suf = samples ? "sample" : "empty";
        return $"questionnaires-template-{tag}-{suf}.xlsx";
    }

    [HttpPost("import")]
    [RequestSizeLimit(25_000_000)]
    [RequestFormLimits(MultipartBodyLengthLimit = 25_000_000)]
    public async Task<IActionResult> Import(IFormFile? file, CancellationToken cancellationToken = default)
    {
        if (file is null || file.Length == 0)
        {
            return BadRequest(Shared.Api.ApiResponse<ExcelImportResultDto>.FromFailure(
                new[] { "الملف مفقود أو فارغ." },
                HttpContext.TraceIdentifier));
        }

        var ext = Path.GetExtension(file.FileName);
        if (!string.Equals(ext, ".xlsx", StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest(Shared.Api.ApiResponse<ExcelImportResultDto>.FromFailure(
                new[] { "يُقبل فقط ملف Excel (.xlsx)." },
                HttpContext.TraceIdentifier));
        }

        await using var stream = file.OpenReadStream();
        var traceId = Activity.Current?.Id ?? HttpContext.TraceIdentifier;
        var result = await _excelBulkDataService.ImportAsync(stream, file.FileName, cancellationToken).ConfigureAwait(false);
        return result.ToApiActionResult(this, traceId);
    }
}
