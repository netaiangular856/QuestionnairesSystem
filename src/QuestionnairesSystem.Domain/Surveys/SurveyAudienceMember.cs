using QuestionnairesSystem.Domain.Common;
using QuestionnairesSystem.Domain.Identity;

namespace QuestionnairesSystem.Domain.Surveys;

/// <summary>
/// يُستخدم عندما يكون <see cref="Survey.AudienceScope"/> = <see cref="Enums.SurveyAudienceScope.SpecificUsers"/>.
/// يحدد الحسابات أو البريد المسموح لهم بالوصول.
/// </summary>
public sealed class SurveyAudienceMember : AuditableDomainEntity
{
    public Guid SurveyId { get; set; }

    /// <summary>موظف أو مستخدم مسجّل في النظام.</summary>
    public Guid? UserId { get; set; }

    /// <summary>متعامل أو طرف خارجي بدون حساب؛ يُكمّل أو يُستخدم بدل UserId.</summary>
    public string? Email { get; set; }

    public Survey Survey { get; set; } = null!;
    public User? User { get; set; }
}
