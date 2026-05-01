/** Mirrors backend domain enums (numeric JSON). */

export enum SurveyStatus {
  Draft = 1,
  PendingApproval = 2,
  Approved = 3,
  Published = 4,
  Closed = 5,
  Rejected = 6,
}

export enum SurveyAudienceScope {
  Everyone = 1,
  Guest = 2,
  SpecificUsers = 3,
  AllOrganizationMembers = 4,
}

export enum RecommendationStatus {
  Draft = 1,
  Active = 2,
  Implemented = 3,
  Dismissed = 4,
}

export enum ActionPlanStatus {
  Draft = 1,
  Active = 2,
  Completed = 3,
  Cancelled = 4,
}

export enum ParticipantStatus {
  Invited = 1,
  Started = 2,
  Completed = 3,
  Declined = 4,
}

export enum ResponseStatus {
  InProgress = 1,
  Submitted = 2,
  Invalid = 3,
}

export interface SurveyListItemDto {
  id: string;
  titleAr: string;
  titleEn: string;
  code: string | null;
  status: SurveyStatus;
  version: number;
  ownerDisplayName: string | null;
  publishedAtUtc: string | null;
  questionCount: number;
  responseCount: number;
}

export interface SurveyDetailDto {
  id: string;
  titleAr: string;
  titleEn: string;
  descriptionAr: string | null;
  descriptionEn: string | null;
  code: string | null;
  status: SurveyStatus;
  audienceScope: SurveyAudienceScope;
  version: number;
  ownerUserId: string | null;
  ownerDisplayName: string | null;
  templateId: string | null;
  publishedAtUtc: string | null;
  closedAtUtc: string | null;
  rejectionReason: string | null;
}

export interface SurveyFilterRequest {
  page: number;
  pageSize: number;
  status?: SurveyStatus | null;
  search?: string | null;
}

export interface CreateSurveyRequest {
  titleAr: string;
  titleEn: string;
  descriptionAr?: string | null;
  descriptionEn?: string | null;
  code?: string | null;
  audienceScope: SurveyAudienceScope;
  templateId?: string | null;
}

export interface UpdateSurveyRequest {
  titleAr: string;
  titleEn: string;
  descriptionAr?: string | null;
  descriptionEn?: string | null;
  code?: string | null;
  audienceScope: SurveyAudienceScope;
}

export interface PatchSurveyStatusRequest {
  status: SurveyStatus;
}

export interface RejectSurveyRequest {
  reason: string;
}

export interface SurveyAnalyticsDto {
  surveyId: string;
  totalResponses: number;
  submittedResponses: number;
  inProgressResponses: number;
  questionCount: number;
  participantCount: number;
}

export interface SurveyAnalyticsSummaryDto {
  surveyId: string;
  completionRate: number;
  submittedCount: number;
  invitedParticipants: number;
}

export interface QuestionAnalyticsItemDto {
  questionId: string;
  titleEn: string;
  answerCount: number;
}

export interface QuestionDto {
  id: string;
  surveyId: string;
  displayOrder: number;
  type: number;
  titleAr: string;
  titleEn: string;
  helpTextAr: string | null;
  helpTextEn: string | null;
  isRequired: boolean;
  optionsJson: string | null;
}

export interface TemplateListItemDto {
  id: string;
  nameAr: string;
  nameEn: string;
  isArchived: boolean;
  usageCount: number;
}

export interface RecommendationDto {
  id: string;
  surveyId: string | null;
  titleAr: string;
  titleEn: string;
  descriptionAr: string | null;
  descriptionEn: string | null;
  priority: number;
  status: RecommendationStatus;
  assignedToUserId: string | null;
  assignedToDisplayName: string | null;
  dueDateUtc: string | null;
}

export interface ActionPlanDto {
  id: string;
  titleAr: string;
  titleEn: string;
  descriptionAr: string | null;
  descriptionEn: string | null;
  surveyId: string | null;
  ownerUserId: string | null;
  ownerDisplayName: string | null;
  status: ActionPlanStatus;
  startDateUtc: string | null;
  endDateUtc: string | null;
}

export interface DashboardReportDto {
  totalSurveys: number;
  publishedSurveys: number;
  totalResponses: number;
  openActionPlans: number;
}

export interface ParticipantDto {
  id: string;
  surveyId: string;
  userId: string | null;
  userDisplayName: string | null;
  email: string | null;
  externalReference: string | null;
  status: ParticipantStatus;
  invitedAtUtc: string | null;
  completedAtUtc: string | null;
}

export interface ResponseListItemDto {
  id: string;
  surveyId: string;
  participantId: string | null;
  respondentDisplayName: string | null;
  status: ResponseStatus;
  startedAtUtc: string | null;
  submittedAtUtc: string | null;
}

export interface AnswerDto {
  questionId: string;
  valueJson: string;
}

export interface ResponseDetailDto {
  id: string;
  surveyId: string;
  participantId: string | null;
  respondentUserId: string | null;
  respondentDisplayName: string | null;
  status: ResponseStatus;
  startedAtUtc: string | null;
  submittedAtUtc: string | null;
  answers: AnswerDto[];
}

export interface ParticipantDetailDto {
  participant: ParticipantDto;
  response: ResponseDetailDto | null;
}
