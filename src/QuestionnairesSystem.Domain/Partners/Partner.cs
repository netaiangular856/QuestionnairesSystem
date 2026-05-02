using QuestionnairesSystem.Domain.Common;
using QuestionnairesSystem.Domain.Enums;

namespace QuestionnairesSystem.Domain.Partners;

public sealed class Partner : AuditableDomainEntity
{
    public string Code { get; set; } = null!;
    public string NameAr { get; set; } = null!;
    public string NameEn { get; set; } = null!;
    public PartnerType Type { get; set; }
    public string? Email { get; set; }
    public string? PhoneNumber { get; set; }
    public string? ContactPerson { get; set; }
    public bool IsActive { get; set; } = true;
    public string? Address { get; set; }
}
