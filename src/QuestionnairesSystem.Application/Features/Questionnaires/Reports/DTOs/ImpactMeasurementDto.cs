namespace QuestionnairesSystem.Application.Features.Questionnaires.Reports.DTOs;

public sealed class ImpactMeasurementFilterRequest
{
    public Guid? SurveyId { get; set; }
    public Guid? ActionPlanId { get; set; }
    public Guid? InitiativeId { get; set; }

    public DateTime? FromUtc { get; set; }
    public DateTime? ToUtc { get; set; }

    /// <summary>Optional UTC instant dividing «before» and «after». When omitted, a heuristic is used (plan start, median timestamps).</summary>
    public DateTime? SplitAtUtc { get; set; }
}

public sealed class ImpactMeasurementBucketDto
{
    public int SurveySubmissionCount { get; init; }
    public double? AverageRating { get; init; }
    public int ProgressEntryCount { get; init; }
    public double? AverageProgressPercent { get; init; }
}

/// <summary>Combined «before / after» view: submitted surveys + initiative execution (each slice optional).</summary>
public sealed class ImpactMeasurementOverviewDto
{
    public ImpactMeasurementDto? SurveyImpact { get; init; }
    public ImpactMeasurementDto? ExecutionImpact { get; init; }
}

public sealed class ImpactMeasurementDto
{
    /// <summary>Survey | AllSurveys | ActionPlan | Initiative | AllExecution</summary>
    public string ScopeKind { get; init; } = string.Empty;

    /// <summary>Empty when <see cref="ScopeKind"/> is an aggregate (<c>AllSurveys</c>, <c>AllExecution</c>).</summary>
    public Guid SubjectId { get; init; }

    public string SubjectTitleAr { get; init; } = string.Empty;
    public string SubjectTitleEn { get; init; } = string.Empty;

    public DateTime SplitAtUtc { get; init; }

    /// <summary>user | planStart | medianTimestamps</summary>
    public string SplitBasis { get; init; } = string.Empty;

    public ImpactMeasurementBucketDto Before { get; init; } = new();
    public ImpactMeasurementBucketDto After { get; init; } = new();

    /// <summary>Current initiative statuses (plan/initiative scope only).</summary>
    public IReadOnlyList<NamedCountDto> InitiativeStatusDistribution { get; init; } = Array.Empty<NamedCountDto>();
}
