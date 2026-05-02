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
  opensAtUtc: string | null;
  closesAtUtc: string | null;
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
  opensAtUtc: string | null;
  closesAtUtc: string | null;
  rejectionReason: string | null;
}

export interface SurveyFilterRequest {
  page: number;
  pageSize: number;
  status?: SurveyStatus | null;
  search?: string | null;
}

/** Matches backend CreateSurveyQuestionItem — type uses backend QuestionType enum values (byte). */
export interface CreateSurveyQuestionItem {
  type: number;
  titleAr: string;
  titleEn: string;
  helpTextAr?: string | null;
  helpTextEn?: string | null;
  isRequired: boolean;
  optionsJson?: string | null;
  displayOrder?: number | null;
}

export interface CreateSurveyRequest {
  titleAr: string;
  titleEn: string;
  descriptionAr?: string | null;
  descriptionEn?: string | null;
  code?: string | null;
  audienceScope: SurveyAudienceScope;
  templateId?: string | null;
  questions?: CreateSurveyQuestionItem[] | null;
  opensAtUtc?: string | null;
  closesAtUtc?: string | null;
}

/** Optional body for POST /publish — audience at publish time. */
export interface PublishSurveyRequest {
  audienceScope?: SurveyAudienceScope | null;
  audienceUserIds?: string[] | null;
}

export interface UpdateSurveyRequest {
  titleAr: string;
  titleEn: string;
  descriptionAr?: string | null;
  descriptionEn?: string | null;
  code?: string | null;
  audienceScope: SurveyAudienceScope;
  questions?: CreateSurveyQuestionItem[] | null;
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

export interface NumericQuestionStatDto {
  questionId: string;
  titleAr: string;
  titleEn: string;
  type: number;
  average: number | null;
  min: number | null;
  max: number | null;
  answerCount: number;
}

export interface SurveyNumericAnalyticsDto {
  surveyId: string;
  questions: NumericQuestionStatDto[];
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

/** Mirrors backend QuestionType (byte). */
export enum QuestionType {
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

export interface TemplateListItemDto {
  id: string;
  nameAr: string;
  nameEn: string;
  isArchived: boolean;
  usageCount: number;
  questionCount: number;
}

export interface TemplateDetailDto {
  id: string;
  nameAr: string;
  nameEn: string;
  descriptionAr: string | null;
  descriptionEn: string | null;
  questions: CreateSurveyQuestionItem[];
  isArchived: boolean;
  usageCount: number;
  questionCount: number;
}

export interface CreateTemplateRequest {
  nameAr: string;
  nameEn: string;
  descriptionAr?: string | null;
  descriptionEn?: string | null;
  questions: CreateSurveyQuestionItem[];
}

export interface UpdateTemplateRequest {
  nameAr: string;
  nameEn: string;
  descriptionAr?: string | null;
  descriptionEn?: string | null;
  questions: CreateSurveyQuestionItem[];
}

export interface UseTemplateResultDto {
  surveyId: string;
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
  questionTitleAr?: string | null;
  questionTitleEn?: string | null;
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

export interface AnswerUpsertDto {
  questionId: string;
  valueJson: string;
}

export interface CreateResponseRequest {
  participantId?: string | null;
  respondentUserId?: string | null;
  answers?: AnswerUpsertDto[] | null;
}

export interface SurveyComprehensiveAnalyticsDto {
  surveyId: string;
  surveyTitle: string;
  overview: SurveyOverviewAnalytics;
  responseTimeline: ResponseTimelineAnalytics[];
  questions: QuestionAnalyticsDto[];
  categories: CategoryAnalyticsDto[];
  ratings: RatingAnalyticsDto[];
}

export interface SurveyOverviewAnalytics {
  totalParticipants: number;
  submittedResponses: number;
  inProgressResponses: number;
  completionRate: number;
  totalQuestions: number;
}

export interface ResponseTimelineAnalytics {
  date: string;
  responseCount: number;
}

export interface QuestionAnalyticsDto {
  questionId: string;
  titleAr: string;
  titleEn: string;
  questionType: string;
  totalAnswers: number;
  answerDistribution: AnswerDistributionDto[];
  averageRating?: number;
  minRating?: number;
  maxRating?: number;
}

export interface AnswerDistributionDto {
  optionText: string;
  count: number;
  percentage: number;
}

export interface CategoryAnalyticsDto {
  categoryName: string;
  responseCount: number;
  percentage: number;
}

export interface RatingAnalyticsDto {
  rating: number;
  count: number;
  percentage: number;
}
