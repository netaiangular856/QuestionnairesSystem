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

/** Matches backend SurveyAudienceSubjectKind — lookup rows for survey audience. */
export enum SurveyAudienceSubjectKind {
  User = 1,
  Employee = 2,
  Partner = 3,
}

/** GET /api/lookups/survey-audience-subjects */
export interface SurveyAudienceLookupItemDto {
  kind: SurveyAudienceSubjectKind;
  entityId: string;
  name: string;
  email: string | null;
  userId: string | null;
}

/** Matches backend SurveyAudienceMemberInputDto */
export interface SurveyAudienceMemberInputDto {
  userId?: string | null;
  email?: string | null;
}

/** Matches backend SurveyAudienceMemberDetailDto */
export interface SurveyAudienceMemberDetailDto {
  userId: string | null;
  email: string | null;
  displayName: string;
}

/** UI chip for audience picker (maps to SurveyAudienceMemberInputDto for API). */
export interface SurveyAudiencePickItem {
  userId: string | null;
  email: string | null;
  label: string;
  kind: SurveyAudienceSubjectKind;
  entityId: string;
}

export enum RecommendationStatus {
  Draft = 1,
  Active = 2,
  Implemented = 3,
  Dismissed = 4,
}

/** UI tier for create/edit forms (maps to numeric priority). */
export type RecommendationPriorityTier = 'low' | 'medium' | 'high';

export enum ActionPlanStatus {
  Draft = 1,
  Active = 2,
  Completed = 3,
  Cancelled = 4,
}

export enum InitiativeStatus {
  Planned = 1,
  InProgress = 2,
  Completed = 3,
  AtRisk = 4,
  Cancelled = 5,
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

/** Matches backend LookupItemDto — used by /api/lookups/*. */
export interface LookupItemDto {
  id: string;
  name: string;
  email: string | null;
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
  showOnPublicPortal?: boolean;
  publicArticleEnabled?: boolean;
  publicArticleTitleAr?: string | null;
  publicArticleTitleEn?: string | null;
  publicArticleBodyAr?: string | null;
  publicArticleBodyEn?: string | null;
  audienceMembers?: SurveyAudienceMemberDetailDto[] | null;
}

/** GET /api/public/surveys — عنصر قائمة البورتال العام. */
export interface PublicSurveyListItemDto {
  id: string;
  titleAr: string;
  titleEn: string;
  code: string;
  descriptionAr: string | null;
  descriptionEn: string | null;
  closesAtUtc: string | null;
}

/** GET /api/public/surveys/{code} — صفحة مقدّمة / تعريف للاستبيان العام. */
export interface PublicSurveyPageDto {
  id: string;
  titleAr: string;
  titleEn: string;
  descriptionAr: string | null;
  descriptionEn: string | null;
  code: string;
  opensAtUtc: string | null;
  closesAtUtc: string | null;
  publicArticleEnabled: boolean;
  publicArticleTitleAr: string | null;
  publicArticleTitleEn: string | null;
  publicArticleBodyAr: string | null;
  publicArticleBodyEn: string | null;
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
  /** Client-only drafts for single/multi choice; mapped to optionsJson on save. */
  choiceOptions?: { labelAr: string; labelEn: string }[] | null;
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
  audienceMembers?: SurveyAudienceMemberInputDto[] | null;
  showOnPublicPortal?: boolean;
  publicArticleEnabled?: boolean;
  publicArticleTitleAr?: string | null;
  publicArticleTitleEn?: string | null;
  publicArticleBodyAr?: string | null;
  publicArticleBodyEn?: string | null;
}

/** Optional body for POST /publish — audience at publish time. */
export interface PublishSurveyRequest {
  audienceScope?: SurveyAudienceScope | null;
  audienceUserIds?: string[] | null;
  audienceMembers?: SurveyAudienceMemberInputDto[] | null;
}

export interface UpdateSurveyRequest {
  titleAr: string;
  titleEn: string;
  descriptionAr?: string | null;
  descriptionEn?: string | null;
  code?: string | null;
  audienceScope: SurveyAudienceScope;
  questions?: CreateSurveyQuestionItem[] | null;
  audienceMembers?: SurveyAudienceMemberInputDto[] | null;
  showOnPublicPortal?: boolean;
  publicArticleEnabled?: boolean;
  publicArticleTitleAr?: string | null;
  publicArticleTitleEn?: string | null;
  publicArticleBodyAr?: string | null;
  publicArticleBodyEn?: string | null;
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

/** POST /api/recommendations */
export interface CreateRecommendationRequest {
  surveyId: string | null;
  titleAr: string;
  titleEn: string;
  descriptionAr?: string | null;
  descriptionEn?: string | null;
  priority: number;
  assignedToUserId?: string | null;
  dueDateUtc?: string | null;
}

/** PUT /api/recommendations/{id} */
export interface UpdateRecommendationRequest {
  titleAr: string;
  titleEn: string;
  descriptionAr?: string | null;
  descriptionEn?: string | null;
  priority: number;
  status: RecommendationStatus;
  assignedToUserId?: string | null;
  dueDateUtc?: string | null;
  surveyId: string | null;
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

/** Row from GET /api/initiatives */
export interface InitiativeListItemDto {
  id: string;
  actionPlanId: string;
  titleAr: string;
  titleEn: string;
  status: InitiativeStatus;
  ownerDisplayName: string | null;
  targetDateUtc: string | null;
  actionPlanTitleAr: string;
  actionPlanTitleEn: string;
}

export interface InitiativeDto {
  id: string;
  actionPlanId: string;
  titleAr: string;
  titleEn: string;
  descriptionAr: string | null;
  descriptionEn: string | null;
  status: InitiativeStatus;
  ownerUserId: string | null;
  ownerDisplayName: string | null;
  targetDateUtc: string | null;
}

export interface InitiativeProgressDto {
  id: string;
  initiativeId: string;
  progressPercent: number | null;
  notes: string | null;
  recordedAtUtc: string;
  recordedByDisplayName: string | null;
}

/** POST /api/action-plans */
export interface CreateActionPlanRequest {
  titleAr: string;
  titleEn: string;
  descriptionAr?: string | null;
  descriptionEn?: string | null;
  surveyId?: string | null;
  ownerUserId?: string | null;
  startDateUtc?: string | null;
  endDateUtc?: string | null;
}

/** PUT /api/action-plans/{id} */
export interface UpdateActionPlanRequest {
  titleAr: string;
  titleEn: string;
  descriptionAr?: string | null;
  descriptionEn?: string | null;
  surveyId?: string | null;
  ownerUserId?: string | null;
  status: ActionPlanStatus;
  startDateUtc?: string | null;
  endDateUtc?: string | null;
}

/** POST /api/action-plans/{id}/initiatives */
export interface CreateInitiativeRequest {
  titleAr: string;
  titleEn: string;
  descriptionAr?: string | null;
  descriptionEn?: string | null;
  ownerUserId?: string | null;
  targetDateUtc?: string | null;
}

/** PUT /api/initiatives/{id} */
export interface UpdateInitiativeRequest {
  titleAr: string;
  titleEn: string;
  descriptionAr?: string | null;
  descriptionEn?: string | null;
  status: InitiativeStatus;
  ownerUserId?: string | null;
  targetDateUtc?: string | null;
}

/** POST /api/initiatives/{id}/progress */
export interface AddInitiativeProgressRequest {
  progressPercent?: number | null;
  notes?: string | null;
}

export interface DepartmentHeadcountRowDto {
  departmentId: string;
  titleAr: string;
  titleEn: string;
  employeeCount: number;
}

/** Query params for GET /api/reports/dashboard — toUtc is exclusive (UTC). */
export interface DashboardFilterQuery {
  fromUtc: string;
  toUtc: string;
}

export interface DashboardReportDto {
  totalSurveys: number;
  publishedSurveys: number;
  totalResponses: number;
  openActionPlans: number;

  totalUsers: number;
  totalInactiveUsers: number;
  totalDepartments: number;
  totalEmployees: number;
  employeesWithoutDepartment: number;
  totalPartners: number;
  totalInitiatives: number;
  totalQuestions: number;
  totalSurveyParticipants: number;
  totalRecommendations: number;
  totalActionPlans: number;
  totalNotifications: number;

  surveyStatusDistribution: NamedCountDto[];
  initiativeStatusDistribution: NamedCountDto[];
  actionPlanStatusDistribution: NamedCountDto[];
  responseStatusDistribution: NamedCountDto[];

  audienceScopeDistribution: NamedCountDto[];
  participantStatusDistribution: NamedCountDto[];
  recommendationStatusDistribution: NamedCountDto[];
  questionTypeDistribution: NamedCountDto[];
  partnerTypeDistribution: NamedCountDto[];
  submissionsByDayOfWeek: NamedCountDto[];

  topDepartmentsByEmployees: DepartmentHeadcountRowDto[];

  submissionsTimelineLast30Days: TimelinePointDto[];
  usersRegisteredTimelineLast30Days: TimelinePointDto[];
  surveysCreatedTimelineLast30Days: TimelinePointDto[];
  actionPlansCreatedTimelineLast30Days: TimelinePointDto[];
  initiativesCreatedTimelineLast30Days: TimelinePointDto[];

  topSurveysBySubmissions: TopSurveyRowDto[];

  /** Echoed when a date filter was applied (UTC). */
  filterFromUtc?: string | null;
  filterToUtcExclusive?: string | null;

  generatedAtUtc: string;
}

/** Workspace-wide survey analytics (filtered by date and/or survey). */
export interface CrossSurveyAnalyticsFilterRequest {
  surveyId?: string | null;
  fromUtc?: string | null;
  toUtc?: string | null;
  /** PDF/Excel language: ar | en */
  lang?: string | null;
  /** When true, API includes every answer row (large payload). Exports set this server-side. */
  includeAnswerDetails?: boolean;
}

/** GET /api/reports/impact-measurement — all filters optional; empty IDs mean workspace-wide aggregates. */
export interface ImpactMeasurementFilterRequest {
  surveyId?: string | null;
  actionPlanId?: string | null;
  initiativeId?: string | null;
  fromUtc?: string | null;
  toUtc?: string | null;
  splitAtUtc?: string | null;
}

export interface ImpactMeasurementBucketDto {
  surveySubmissionCount: number;
  averageRating: number | null;
  progressEntryCount: number;
  averageProgressPercent: number | null;
}

export interface ImpactMeasurementDto {
  /** Survey | AllSurveys | ActionPlan | Initiative | AllExecution */
  scopeKind: string;
  subjectId: string;
  subjectTitleAr: string;
  subjectTitleEn: string;
  splitAtUtc: string;
  splitBasis: string;
  before: ImpactMeasurementBucketDto;
  after: ImpactMeasurementBucketDto;
  initiativeStatusDistribution: NamedCountDto[];
}

/** GET /api/reports/impact-measurement — overview: surveys + execution (either may be null). */
export interface ImpactMeasurementOverviewDto {
  surveyImpact: ImpactMeasurementDto | null;
  executionImpact: ImpactMeasurementDto | null;
}

export interface CrossSurveyAnalyticsDto {
  appliedFilter: CrossSurveyAnalyticsFilterSnapshotDto;
  overview: CrossSurveyOverviewDto;
  surveyStatusDistribution: NamedCountDto[];
  responseStatusDistribution: NamedCountDto[];
  /** Rich analytics — present when API is updated. */
  audienceScopeDistribution?: NamedCountDto[];
  participantStatusDistribution?: NamedCountDto[];
  submissionsByDayOfWeek?: NamedCountDto[];
  submissionsByDay: TimelinePointDto[];
  topSurveysBySubmissions: TopSurveyRowDto[];
  actionPlanStatusDistribution?: NamedCountDto[];
  initiativeStatusDistribution?: NamedCountDto[];
  actionPlans?: CrossSurveyActionPlanReportRowDto[];
  initiatives?: CrossSurveyInitiativeReportRowDto[];
  /** Rating/scale answers aggregated for the same submitted-response scope as the filter. */
  ratingsDistribution?: RatingAnalyticsDto[];
  /** Answer counts per question type (submitted responses in scope). */
  questionTypeAnswerTotals?: NamedCountDto[];
  /** Frequent tokens from ShortText/LongText (same scope). */
  textAnswerKeywords?: KeywordCountDto[];
  /** Flat rows: one per question answer in submitted responses (only when includeAnswerDetails). */
  answerDetails?: CrossSurveyAnswerDetailRowDto[];
}

/** One answer cell for full-data export / optional JSON analytics payload. */
export interface CrossSurveyAnswerDetailRowDto {
  surveyTitleAr: string;
  surveyTitleEn: string;
  submittedAtUtc: string | null;
  respondentDisplayName: string | null;
  questionTitleAr: string;
  questionTitleEn: string;
  questionTypeKey: string;
  answerTextAr: string;
  answerTextEn: string;
}

export interface CrossSurveyAnalyticsFilterSnapshotDto {
  surveyId: string | null;
  fromUtc: string | null;
  toUtc: string | null;
  surveyTitleAr?: string | null;
  surveyTitleEn?: string | null;
}

export interface CrossSurveyOverviewDto {
  surveysInScope: number;
  publishedSurveys: number;
  submittedResponsesInPeriod: number;
  surveysWithSubmissionsInPeriod: number;
  invitedParticipantsInScope: number;
  inProgressResponsesOpen: number;
  submissionsPerDayInPeriod: number;
  totalQuestionsInScope: number;
  completedParticipantsInScope?: number;
  declinedParticipantsInScope?: number;
  averageMinutesToSubmitInPeriod?: number;
  actionPlansInScope?: number;
  initiativesInScope?: number;
}

export interface CrossSurveyActionPlanReportRowDto {
  titleAr: string;
  titleEn: string;
  status: string;
  linkedSurveyTitleAr?: string | null;
  linkedSurveyTitleEn?: string | null;
  createdOnUtc: string;
}

export interface CrossSurveyInitiativeReportRowDto {
  titleAr: string;
  titleEn: string;
  status: string;
  actionPlanTitleAr: string;
  actionPlanTitleEn: string;
  linkedSurveyTitleAr?: string | null;
  linkedSurveyTitleEn?: string | null;
  targetDateUtc?: string | null;
  createdOnUtc: string;
}

export interface NamedCountDto {
  key: string;
  count: number;
}

export interface TimelinePointDto {
  date: string;
  count: number;
}

export interface TopSurveyRowDto {
  surveyId: string;
  titleAr: string;
  titleEn: string;
  status: string;
  submissionsInPeriod: number;
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

export interface KeywordCountDto {
  keyword: string;
  count: number;
}

export interface SurveyComprehensiveAnalyticsDto {
  surveyId: string;
  surveyTitle: string;
  overview: SurveyOverviewAnalytics;
  responseTimeline: ResponseTimelineAnalytics[];
  questions: QuestionAnalyticsDto[];
  categories: CategoryAnalyticsDto[];
  ratings: RatingAnalyticsDto[];
  textAnswerKeywords?: KeywordCountDto[];
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
  /** Localized label (API). When set, use with optionTextEn for the active UI language. */
  optionTextAr?: string;
  optionTextEn?: string;
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

/** POST /api/ai */
export interface TranslateRichTextRequest {
  html: string;
  sourceLang: 'ar' | 'en';
  targetLang: 'ar' | 'en';
}

export interface TranslateRichTextResponse {
  html: string;
}

export interface AiSuggestFromSurveyRequest {
  surveyId?: string | null;
}

export interface AiSuggestRecommendationDraftDto {
  titleAr: string;
  titleEn: string;
  descriptionAr?: string | null;
  descriptionEn?: string | null;
  priority: number;
}

export interface AiSuggestActionPlanDraftDto {
  titleAr: string;
  titleEn: string;
  descriptionAr?: string | null;
  descriptionEn?: string | null;
}

export interface AiAnalyzeReportsRequest {
  filter: CrossSurveyAnalyticsFilterRequest;
}

export interface AiChartSuggestionDto {
  /** Legacy single title (often English). Prefer titleAr / titleEn. */
  title?: string;
  titleAr?: string | null;
  titleEn?: string | null;
  /** Server returns bar | line | doughnut */
  kind?: string | null;
  labels: string[];
  values: number[];
}

export interface AiKpiChipDto {
  labelAr: string;
  labelEn: string;
  valueText: string;
  hintAr?: string | null;
  hintEn?: string | null;
}

export type AiInsightCardKind =
  | 'risk_detected'
  | 'low_satisfaction'
  | 'improvement_opportunity'
  | 'executive_insight'
  | 'recommended_action'
  | 'sentiment_summary';

export interface AiInsightCardDto {
  kind: string;
  titleAr: string;
  titleEn: string;
  bodyAr: string;
  bodyEn: string;
  severity?: string | null;
}

export interface AiAnalyzeReportsResponseDto {
  summaryAr: string;
  summaryEn: string;
  charts: AiChartSuggestionDto[];
  kpis?: AiKpiChipDto[] | null;
  insightCards?: AiInsightCardDto[] | null;
  recommendationsAr?: string[] | null;
  recommendationsEn?: string[] | null;
  executiveBoxAr?: string | null;
  executiveBoxEn?: string | null;
  actionPlanStepsAr?: string[] | null;
  actionPlanStepsEn?: string[] | null;
}

export interface AiGenerateSurveyRequest {
  briefAr?: string | null;
  briefEn?: string | null;
  maxQuestions?: number | null;
}

/** Server: AI reads recommendations + recent survey titles, then persists a new draft survey. */
export interface AiAutoSurveyFromRecommendationsRequest {
  maxQuestions?: number | null;
  maxRecommendations?: number | null;
  recentSurveyCount?: number | null;
}

export interface AiAutoCreateSurveyResponseDto {
  surveyId: string;
  titleAr: string;
  titleEn: string;
}

export interface AiGeneratedOptionDraftDto {
  textAr: string;
  textEn: string;
}

export interface AiGeneratedQuestionDraftDto {
  type: string;
  titleAr: string;
  titleEn: string;
  required: boolean;
  options?: AiGeneratedOptionDraftDto[] | null;
}

export interface AiGeneratedSurveyDraftDto {
  titleAr: string;
  titleEn: string;
  descriptionAr?: string | null;
  descriptionEn?: string | null;
  questions: AiGeneratedQuestionDraftDto[];
}

export interface AiSentimentAnalysisRequest {
  surveyId?: string | null;
  analyticsFilter?: CrossSurveyAnalyticsFilterRequest | null;
  defaultWindowDays?: number | null;
}

export interface AiSentimentMixDto {
  positive: number;
  negative: number;
  neutral: number;
}

export interface AiSentimentAnalysisResponseDto {
  summaryAr: string;
  summaryEn: string;
  overallToneAr?: string | null;
  overallToneEn?: string | null;
  sentimentMix?: AiSentimentMixDto | null;
  /** Optional bar/line chart from the same excerpt analysis (e.g. by question). */
  insightChart?: AiChartSuggestionDto | null;
  cards: AiInsightCardDto[];
}

export interface AiCopilotMessageDto {
  role: string;
  content: string;
}

export interface AiCopilotChatRequest {
  surveyId?: string | null;
  analyticsFilter?: CrossSurveyAnalyticsFilterRequest | null;
  history?: AiCopilotMessageDto[] | null;
  userMessage: string;
}

export interface AiCopilotChatResponseDto {
  replyAr: string;
  replyEn: string;
  insightCards: AiInsightCardDto[];
  suggestedPromptsAr: string[];
  suggestedPromptsEn: string[];
}
