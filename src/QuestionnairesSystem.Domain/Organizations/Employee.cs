using QuestionnairesSystem.Domain.Common;
using QuestionnairesSystem.Domain.Identity;

namespace QuestionnairesSystem.Domain.Organizations;

public sealed class Employee : AuditableDomainEntity
{
    public string EmployeeNumber { get; set; } = null!;
    public string NameAr { get; set; } = null!;
    public string NameEn { get; set; } = null!;
    public string? Email { get; set; }
    public string? PhoneNumber { get; set; }
    public string? JobTitleAr { get; set; }
    public string? JobTitleEn { get; set; }
    public Guid? DepartmentId { get; set; }
    public bool IsActive { get; set; } = true;

    public Department? Department { get; set; }
    public User? User { get; set; }
}
