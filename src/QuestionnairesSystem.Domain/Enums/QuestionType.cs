namespace QuestionnairesSystem.Domain.Enums;

/// <summary>
/// ثلاثة أنواع أسئلة: مقياس رقمي، نص حر، أو اختيار من خيارات.
/// التفاصيل (نطاق المقياس، الخيارات) تُخزَّن في حقل OptionsJson على السؤال.
/// </summary>
public enum QuestionType : byte
{
    ShortText = 1,
    LongText = 2,
    SingleChoice = 3,
    MultipleChoice = 4,
    Rating = 5,
    Scale = 6,
    YesNo = 7,
    Date = 8,
    Number = 9,
}
