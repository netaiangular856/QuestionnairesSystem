using QuestionnairesSystem.Domain.Common;

namespace QuestionnairesSystem.Domain.Organizations;

public sealed class Department : AuditableDomainEntity
{
    public string Code { get; set; } = null!;
    public string NameAr { get; set; } = null!;
    public string NameEn { get; set; } = null!;
    public Guid? ParentDepartmentId { get; set; }

    public Department? ParentDepartment { get; set; }
    public ICollection<Department> Children { get; set; } = new List<Department>();
}
