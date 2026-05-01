namespace QuestionnairesSystem.Domain.Enums;

/// <summary>
/// من يستطيع رؤية الاستبيان والإجابة عليه؛ يحدده منشئ الاستبيان.
/// </summary>
public enum SurveyAudienceScope : byte
{
    /// <summary>متاح للجميع (مثلاً رابط عام).</summary>
    Everyone = 1,

    /// <summary>ضيف / بدون حساب أو رابط مدعوم بالضيف حسب سياسة التطبيق.</summary>
    Guest = 2,

    /// <summary>فقط مستخدمون مُدرَجون في <see cref="Surveys.SurveyAudienceMember"/> (موظفون محددون، شركاء ببريد، إلخ).</summary>
    SpecificUsers = 3,

    /// <summary>كل من له حساب في المنظمة دون إدراج أسماء واحدة واحدة.</summary>
    AllOrganizationMembers = 4,
}
