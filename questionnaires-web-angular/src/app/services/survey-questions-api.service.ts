import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { map } from 'rxjs/operators';
import { apiUrl } from '../core/config/api-url';
import { ApiResponse } from '../shared/models/api.types';
import { QuestionDto } from '../shared/models/questionnaire.models';
import { unwrapApiResponse } from '../shared/utils/api-helpers';

@Injectable({ providedIn: 'root' })
export class SurveyQuestionsApiService {
  private readonly http = inject(HttpClient);

  list(surveyId: string) {
    const base = apiUrl(`/api/surveys/${surveyId}/questions`);
    return this.http.get<ApiResponse<QuestionDto[]>>(base).pipe(map((r) => unwrapApiResponse(r)));
  }
}
