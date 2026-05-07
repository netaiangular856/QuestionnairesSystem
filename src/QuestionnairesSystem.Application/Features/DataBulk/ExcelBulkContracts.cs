namespace QuestionnairesSystem.Application.Features.DataBulk;

/// <summary>أي جداول تُدرَج في ملف Excel المُصدَر.</summary>
public enum ExcelTemplateScope
{
    All = 0,
    Departments = 1,
    Employees = 2,
    Partners = 3,
    Users = 4,
    /// <summary>Surveys + SurveyQuestions + SurveyResponses في مصنف واحد.</summary>
    Surveys = 5,
    /// <summary>قوالب الاستبيان: SurveyTemplates + SurveyTemplateQuestions.</summary>
    Templates = 6,
    /// <summary>التوصيات (ورقة Recommendations فقط).</summary>
    Recommendations = 7,
    /// <summary>خطط العمل (ورقة ActionPlans فقط).</summary>
    ActionPlans = 8,
    /// <summary>المبادرات: ActionPlans + Initiatives في مصنف واحد (المبادرة تحتاج ActionPlanKey).</summary>
    Initiatives = 9,
}

public sealed class ExcelImportResultDto
{
    public int DepartmentsImported { get; init; }
    public int EmployeesImported { get; init; }
    public int PartnersImported { get; init; }
    public int UsersImported { get; init; }
    public int SurveysImported { get; init; }
    public int TemplatesImported { get; init; }
    public int ResponsesImported { get; init; }
    public int RecommendationsImported { get; init; }
    public int ActionPlansImported { get; init; }
    public int InitiativesImported { get; init; }
    public IReadOnlyList<ExcelImportRowErrorDto> Errors { get; init; } = Array.Empty<ExcelImportRowErrorDto>();
}

public sealed class ExcelImportRowErrorDto
{
    public string Sheet { get; init; } = "";
    public int RowNumber { get; init; }
    public string Message { get; init; } = "";
}
