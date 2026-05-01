using QuestionnairesSystem.Domain.Common;

namespace QuestionnairesSystem.Domain.Settings;

public sealed class AppSetting : AuditableDomainEntity
{
    public string Key { get; set; } = null!;
    public string Value { get; set; } = null!;
    public string? Description { get; set; }
    public string? Category { get; set; }
}
