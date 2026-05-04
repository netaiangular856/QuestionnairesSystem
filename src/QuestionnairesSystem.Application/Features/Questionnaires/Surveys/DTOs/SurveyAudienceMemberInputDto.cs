namespace QuestionnairesSystem.Application.Features.Questionnaires.Surveys.DTOs;

/// <summary>عنصر جمهور: إما مستخدم مسجّل أو بريد (متعامل / موظف بلا حساب).</summary>
public sealed class SurveyAudienceMemberInputDto
{
    public Guid? UserId { get; set; }

    public string? Email { get; set; }
}
