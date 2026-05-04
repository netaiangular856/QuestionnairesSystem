namespace QuestionnairesSystem.Application.Features.Questionnaires.Reports.DTOs;

public sealed class DashboardReportDto
{
    public int TotalSurveys { get; init; }
    public int PublishedSurveys { get; init; }
    public int TotalResponses { get; init; }
    public int OpenActionPlans { get; init; }

    public int TotalUsers { get; init; }
    /// <summary>Users with <c>IsActive == false</c>.</summary>
    public int TotalInactiveUsers { get; init; }
    public int TotalDepartments { get; init; }
    public int TotalEmployees { get; init; }
    /// <summary>Employees with no <see cref="Organizations.Department"/> assigned.</summary>
    public int EmployeesWithoutDepartment { get; init; }
    public int TotalPartners { get; init; }
    public int TotalInitiatives { get; init; }
    public int TotalQuestions { get; init; }
    public int TotalSurveyParticipants { get; init; }
    public int TotalRecommendations { get; init; }
    public int TotalActionPlans { get; init; }
    public int TotalNotifications { get; init; }

    public IReadOnlyList<NamedCountDto> SurveyStatusDistribution { get; init; } = Array.Empty<NamedCountDto>();
    public IReadOnlyList<NamedCountDto> InitiativeStatusDistribution { get; init; } = Array.Empty<NamedCountDto>();
    public IReadOnlyList<NamedCountDto> ActionPlanStatusDistribution { get; init; } = Array.Empty<NamedCountDto>();
    public IReadOnlyList<NamedCountDto> ResponseStatusDistribution { get; init; } = Array.Empty<NamedCountDto>();

    public IReadOnlyList<NamedCountDto> AudienceScopeDistribution { get; init; } = Array.Empty<NamedCountDto>();
    public IReadOnlyList<NamedCountDto> ParticipantStatusDistribution { get; init; } = Array.Empty<NamedCountDto>();
    public IReadOnlyList<NamedCountDto> RecommendationStatusDistribution { get; init; } = Array.Empty<NamedCountDto>();
    public IReadOnlyList<NamedCountDto> QuestionTypeDistribution { get; init; } = Array.Empty<NamedCountDto>();
    public IReadOnlyList<NamedCountDto> PartnerTypeDistribution { get; init; } = Array.Empty<NamedCountDto>();

    /// <summary>Submitted responses in the last 365 days, grouped by <see cref="DayOfWeek"/> (UTC).</summary>
    public IReadOnlyList<NamedCountDto> SubmissionsByDayOfWeek { get; init; } = Array.Empty<NamedCountDto>();

    public IReadOnlyList<DepartmentHeadcountRowDto> TopDepartmentsByEmployees { get; init; } = Array.Empty<DepartmentHeadcountRowDto>();

    /// <summary>Submitted responses per calendar day (UTC), last 30 days (inclusive).</summary>
    public IReadOnlyList<TimelinePointDto> SubmissionsTimelineLast30Days { get; init; } = Array.Empty<TimelinePointDto>();

    public IReadOnlyList<TimelinePointDto> UsersRegisteredTimelineLast30Days { get; init; } = Array.Empty<TimelinePointDto>();
    public IReadOnlyList<TimelinePointDto> SurveysCreatedTimelineLast30Days { get; init; } = Array.Empty<TimelinePointDto>();
    public IReadOnlyList<TimelinePointDto> ActionPlansCreatedTimelineLast30Days { get; init; } = Array.Empty<TimelinePointDto>();
    public IReadOnlyList<TimelinePointDto> InitiativesCreatedTimelineLast30Days { get; init; } = Array.Empty<TimelinePointDto>();

    /// <summary>Surveys with the most submitted responses (all time).</summary>
    public IReadOnlyList<TopSurveyRowDto> TopSurveysBySubmissions { get; init; } = Array.Empty<TopSurveyRowDto>();

    /// <summary>When set, KPIs and charts use this inclusive UTC lower bound.</summary>
    public DateTime? FilterFromUtc { get; init; }

    /// <summary>When set, exclusive UTC upper bound applied as <c>&lt; FilterToUtcExclusive</c>.</summary>
    public DateTime? FilterToUtcExclusive { get; init; }

    public DateTime GeneratedAtUtc { get; init; }
}
