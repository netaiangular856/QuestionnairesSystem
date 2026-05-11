namespace QuestionnairesSystem.Application.Common;

public static class QuestionnaireErrors
{
    public const string SurveyNotFound = "QUESTIONNAIRE_SURVEY_NOT_FOUND";
    public const string SurveyCodeAlreadyExists = "QUESTIONNAIRE_SURVEY_CODE_DUPLICATE";
    public const string QuestionNotFound = "QUESTIONNAIRE_QUESTION_NOT_FOUND";
    public const string TemplateNotFound = "QUESTIONNAIRE_TEMPLATE_NOT_FOUND";
    public const string ParticipantNotFound = "QUESTIONNAIRE_PARTICIPANT_NOT_FOUND";
    public const string ResponseNotFound = "QUESTIONNAIRE_RESPONSE_NOT_FOUND";
    public const string RecommendationNotFound = "QUESTIONNAIRE_RECOMMENDATION_NOT_FOUND";
    public const string ActionPlanNotFound = "QUESTIONNAIRE_ACTION_PLAN_NOT_FOUND";
    public const string InitiativeNotFound = "QUESTIONNAIRE_INITIATIVE_NOT_FOUND";
    public const string InvalidStatusTransition = "QUESTIONNAIRE_INVALID_STATUS";
    public const string InvalidOperation = "QUESTIONNAIRE_INVALID_OPERATION";
    public const string ImpactMeasurementInsufficientData = "QUESTIONNAIRE_IMPACT_MEASUREMENT_INSUFFICIENT_DATA";

    public const string AiNotConfigured = "QUESTIONNAIRE_AI_NOT_CONFIGURED";

    public const string AiProviderError = "QUESTIONNAIRE_AI_PROVIDER_ERROR";

    /// <summary>Missing survey, empty user prompt, or analytics scope too thin for the requested AI operation.</summary>
    public const string AiInsufficientData = "QUESTIONNAIRE_AI_INSUFFICIENT_DATA";
}
