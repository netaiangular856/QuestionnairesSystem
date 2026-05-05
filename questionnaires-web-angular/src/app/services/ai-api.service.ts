import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { map } from 'rxjs/operators';
import { apiUrl } from '../core/config/api-url';
import { ApiResponse } from '../shared/models/api.types';
import {
  AiAnalyzeReportsRequest,
  AiAnalyzeReportsResponseDto,
  AiSuggestActionPlanDraftDto,
  AiSuggestFromSurveyRequest,
  AiSuggestRecommendationDraftDto,
  CrossSurveyAnalyticsFilterRequest,
  TranslateRichTextRequest,
  TranslateRichTextResponse,
} from '../shared/models/questionnaire.models';
import { unwrapApiResponse } from '../shared/utils/api-helpers';
import { stripMarkdownCodeFences } from '../shared/utils/bilingual-ai-translate';

@Injectable({ providedIn: 'root' })
export class AiApiService {
  private readonly http = inject(HttpClient);
  private readonly base = apiUrl('/api/ai');

  translateRichText(body: TranslateRichTextRequest): Observable<TranslateRichTextResponse> {
    return this.http.post<ApiResponse<TranslateRichTextResponse>>(`${this.base}/translate-rich-text`, body).pipe(
      map((r) => unwrapApiResponse(r)),
      map((resp) => ({
        ...resp,
        html: stripMarkdownCodeFences(resp.html ?? ''),
      }))
    );
  }

  suggestRecommendation(body: AiSuggestFromSurveyRequest = {}): Observable<AiSuggestRecommendationDraftDto> {
    return this.http
      .post<ApiResponse<AiSuggestRecommendationDraftDto>>(`${this.base}/suggest-recommendation`, body)
      .pipe(map((r) => unwrapApiResponse(r)));
  }

  suggestActionPlan(body: AiSuggestFromSurveyRequest = {}): Observable<AiSuggestActionPlanDraftDto> {
    return this.http
      .post<ApiResponse<AiSuggestActionPlanDraftDto>>(`${this.base}/suggest-action-plan`, body)
      .pipe(map((r) => unwrapApiResponse(r)));
  }

  analyzeReports(filter: CrossSurveyAnalyticsFilterRequest): Observable<AiAnalyzeReportsResponseDto> {
    const req: AiAnalyzeReportsRequest = { filter };
    return this.http
      .post<ApiResponse<AiAnalyzeReportsResponseDto>>(`${this.base}/analyze-reports`, req)
      .pipe(map((r) => unwrapApiResponse(r)));
  }

  /** Same query shape as reports analytics for convenience */
  analyzeReportsFromQuery(params: Record<string, string | undefined>): Observable<AiAnalyzeReportsResponseDto> {
    const filter: CrossSurveyAnalyticsFilterRequest = {};
    const sid = params['surveyId'];
    if (sid) filter.surveyId = sid;
    const from = params['fromUtc'];
    const to = params['toUtc'];
    if (from) filter.fromUtc = from;
    if (to) filter.toUtc = to;
    return this.analyzeReports(filter);
  }
}
