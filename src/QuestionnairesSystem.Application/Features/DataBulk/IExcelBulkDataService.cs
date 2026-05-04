using QuestionnairesSystem.Shared.Results;

namespace QuestionnairesSystem.Application.Features.DataBulk;

public interface IExcelBulkDataService
{
    Task<byte[]> GetTemplateAsync(ExcelTemplateScope scope, bool includeSamples, CancellationToken cancellationToken = default);

    Task<Result<ExcelImportResultDto>> ImportAsync(Stream stream, string fileName, CancellationToken cancellationToken = default);
}
