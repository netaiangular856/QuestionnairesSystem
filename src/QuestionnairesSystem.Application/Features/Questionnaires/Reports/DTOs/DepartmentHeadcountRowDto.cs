namespace QuestionnairesSystem.Application.Features.Questionnaires.Reports.DTOs;

/// <summary>Department size for dashboard charts (no sensitive employee lists).</summary>
public sealed class DepartmentHeadcountRowDto
{
    public Guid DepartmentId { get; init; }
    public string TitleAr { get; init; } = string.Empty;
    public string TitleEn { get; init; } = string.Empty;
    public int EmployeeCount { get; init; }
}
