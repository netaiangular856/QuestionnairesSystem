using QuestionnairesSystem.Application.Features.Questionnaires.Surveys.Interfaces;

namespace QuestionnairesSystem.Api.Services;

public sealed class SurveyScheduleHostedService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<SurveyScheduleHostedService> _logger;

    public SurveyScheduleHostedService(IServiceScopeFactory scopeFactory, ILogger<SurveyScheduleHostedService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken).ConfigureAwait(false);
                using var scope = _scopeFactory.CreateScope();
                var surveys = scope.ServiceProvider.GetRequiredService<ISurveyService>();
                var result = await surveys.CloseExpiredPublishedSurveysAsync(stoppingToken).ConfigureAwait(false);
                if (result.IsSuccess && result.Value > 0)
                    _logger.LogInformation("Auto-closed {Count} survey(s) after schedule end.", result.Value);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Survey schedule sweep failed.");
            }
        }
    }
}
