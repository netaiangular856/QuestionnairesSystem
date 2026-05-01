using QuestionnairesSystem.Domain.Common;

namespace QuestionnairesSystem.Domain.Templates;

public sealed class SurveyTemplate : AuditableDomainEntity
{
    public string NameAr { get; set; } = null!;
    public string NameEn { get; set; } = null!;
    public string? DescriptionAr { get; set; }
    public string? DescriptionEn { get; set; }
    /// <summary>Serialized survey structure (questions snapshot).</summary>
    public string StructureJson { get; set; } = "{}";
    public bool IsArchived { get; set; }
    public int UsageCount { get; set; }

    public ICollection<Surveys.Survey> Surveys { get; set; } = new List<Surveys.Survey>();
}
