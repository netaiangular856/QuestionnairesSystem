import { AppLang } from '../i18n/translations';
import {
  ActionPlanStatus,
  ParticipantStatus,
  RecommendationStatus,
  ResponseStatus,
  SurveyAudienceScope,
  SurveyStatus,
} from '../models/questionnaire.models';

export function qLocalizedTitle(lang: AppLang, ar: string, en: string): string {
  return lang === 'ar' ? ar : en;
}

export function qSurveyStatusKey(status: SurveyStatus): string {
  switch (status) {
    case SurveyStatus.Draft:
      return 'q.surveyStatus.draft';
    case SurveyStatus.PendingApproval:
      return 'q.surveyStatus.pending';
    case SurveyStatus.Approved:
      return 'q.surveyStatus.approved';
    case SurveyStatus.Published:
      return 'q.surveyStatus.published';
    case SurveyStatus.Closed:
      return 'q.surveyStatus.closed';
    case SurveyStatus.Rejected:
      return 'q.surveyStatus.rejected';
    default:
      return 'q.surveyStatus.draft';
  }
}

export function qAudienceKey(scope: SurveyAudienceScope): string {
  switch (scope) {
    case SurveyAudienceScope.Everyone:
      return 'q.audience.everyone';
    case SurveyAudienceScope.Guest:
      return 'q.audience.guest';
    case SurveyAudienceScope.SpecificUsers:
      return 'q.audience.specific';
    case SurveyAudienceScope.AllOrganizationMembers:
      return 'q.audience.org';
    default:
      return 'q.audience.org';
  }
}

export function qRecStatusKey(status: RecommendationStatus): string {
  switch (status) {
    case RecommendationStatus.Draft:
      return 'q.recStatus.draft';
    case RecommendationStatus.Active:
      return 'q.recStatus.active';
    case RecommendationStatus.Implemented:
      return 'q.recStatus.implemented';
    case RecommendationStatus.Dismissed:
      return 'q.recStatus.dismissed';
    default:
      return 'q.recStatus.draft';
  }
}

export function qPlanStatusKey(status: ActionPlanStatus): string {
  switch (status) {
    case ActionPlanStatus.Draft:
      return 'q.planStatus.draft';
    case ActionPlanStatus.Active:
      return 'q.planStatus.active';
    case ActionPlanStatus.Completed:
      return 'q.planStatus.completed';
    case ActionPlanStatus.Cancelled:
      return 'q.planStatus.cancelled';
    default:
      return 'q.planStatus.draft';
  }
}

export function qParticipantStatusKey(status: ParticipantStatus): string {
  switch (status) {
    case ParticipantStatus.Invited:
      return 'q.participantStatus.invited';
    case ParticipantStatus.Started:
      return 'q.participantStatus.started';
    case ParticipantStatus.Completed:
      return 'q.participantStatus.completed';
    case ParticipantStatus.Declined:
      return 'q.participantStatus.declined';
    default:
      return 'q.participantStatus.invited';
  }
}

export function qResponseStatusKey(status: ResponseStatus): string {
  switch (status) {
    case ResponseStatus.InProgress:
      return 'q.responseStatus.inProgress';
    case ResponseStatus.Submitted:
      return 'q.responseStatus.submitted';
    case ResponseStatus.Invalid:
      return 'q.responseStatus.invalid';
    default:
      return 'q.responseStatus.inProgress';
  }
}
