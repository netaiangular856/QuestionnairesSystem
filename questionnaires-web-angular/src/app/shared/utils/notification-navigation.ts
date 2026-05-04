import { NotificationDto } from '../models/notification.models';

/** Must match backend `NotificationRelatedEntityTypes` (Application layer). */
const Survey = 'Survey';
const SurveyTemplate = 'SurveyTemplate';
const Recommendation = 'Recommendation';
const ActionPlan = 'ActionPlan';
const Initiative = 'Initiative';
const SurveyResponse = 'SurveyResponse';
const SurveyParticipant = 'SurveyParticipant';

/**
 * Returns an in-app router URL for the entity linked to a notification, or null if none.
 */
export function notificationTargetUrl(n: NotificationDto): string | null {
  const type = n.relatedEntityType;
  const id = n.relatedEntityId;
  const parent = n.relatedEntityParentId;
  if (!type || !id) return null;

  switch (type) {
    case Survey:
      return `/surveys/${id}`;
    case SurveyTemplate:
      return `/templates/${id}`;
    case Recommendation:
      return `/recommendations?recommendationId=${encodeURIComponent(id)}`;
    case ActionPlan:
      return `/action-plans/${id}`;
    case Initiative:
      return `/initiatives/${id}`;
    case SurveyResponse:
      return parent ? `/surveys/${parent}/responses/${id}` : null;
    case SurveyParticipant:
      return parent ? `/surveys/${parent}/participants/${id}` : null;
    default:
      return null;
  }
}
