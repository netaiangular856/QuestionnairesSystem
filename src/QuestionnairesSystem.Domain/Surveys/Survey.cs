using QuestionnairesSystem.Domain.Common;
using QuestionnairesSystem.Domain.Enums;
using QuestionnairesSystem.Domain.Identity;
using QuestionnairesSystem.Domain.Templates;

namespace QuestionnairesSystem.Domain.Surveys;

public sealed class Survey : AuditableDomainEntity
{
    public string TitleAr { get; set; } = null!;
    public string TitleEn { get; set; } = null!;
    public string? DescriptionAr { get; set; }
    public string? DescriptionEn { get; set; }
    public string? Code { get; set; }
    public SurveyStatus Status { get; set; } = SurveyStatus.Draft;
    /// <summary>من يصل للاستبيان: الكل، ضيف، قائمة محددة، أو كل أعضاء المنظمة.</summary>
    public SurveyAudienceScope AudienceScope { get; set; } = SurveyAudienceScope.AllOrganizationMembers;
    public int Version { get; set; } = 1;
    public Guid? OwnerUserId { get; set; }
    public Guid? TemplateId { get; set; }
    public DateTime? PublishedAtUtc { get; set; }
    public DateTime? ClosedAtUtc { get; set; }
    /// <summary>بداية المخطط الزمني (اختياري؛ يُستخدم مع النشر والأرشفة).</summary>
    public DateTime? OpensAtUtc { get; set; }
    /// <summary>نهاية المخطط؛ عند تجاوزها يُغلق الاستبيان تلقائياً (أرشفة).</summary>
    public DateTime? ClosesAtUtc { get; set; }
    public string? RejectionReason { get; set; }

    /// <summary>يظهر في صفحة الجمهور العامة ضمن قائمة الاستبيانات المتاحة (مع استيفاء شروط النشر والجمهور).</summary>
    public bool ShowOnPublicPortal { get; set; }

    /// <summary>تفعيل صفحة مقدّمة (مقال) قبل الدخول لملء الاستبيان من الرابط العام.</summary>
    public bool PublicArticleEnabled { get; set; }

    public string? PublicArticleTitleAr { get; set; }
    public string? PublicArticleTitleEn { get; set; }
    public string? PublicArticleBodyAr { get; set; }
    public string? PublicArticleBodyEn { get; set; }

    public User? Owner { get; set; }
    public SurveyTemplate? Template { get; set; }
    public ICollection<Question> Questions { get; set; } = new List<Question>();
    public ICollection<SurveyAudienceMember> AudienceMembers { get; set; } = new List<SurveyAudienceMember>();
    public ICollection<Participants.SurveyParticipant> Participants { get; set; } = new List<Participants.SurveyParticipant>();
    public ICollection<Responses.SurveyResponse> Responses { get; set; } = new List<Responses.SurveyResponse>();
    public ICollection<Recommendations.Recommendation> Recommendations { get; set; } = new List<Recommendations.Recommendation>();
    public ICollection<ActionPlans.ActionPlan> ActionPlans { get; set; } = new List<ActionPlans.ActionPlan>();
}
