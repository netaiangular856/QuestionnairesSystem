using QuestionnairesSystem.Domain.Common;

namespace QuestionnairesSystem.Domain.Lookups;

public sealed class LookupItem : AuditableDomainEntity
{
    public string Category { get; set; } = null!;
    public string Code { get; set; } = null!;
    public string NameAr { get; set; } = null!;
    public string NameEn { get; set; } = null!;
    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;
}
