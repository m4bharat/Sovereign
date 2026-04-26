import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { SuggestionAnalyticsResponse, SuggestionFailureResponse, SuggestionMetricBucket } from '../models/telemetry.models';

@Injectable({ providedIn: 'root' })
export class TelemetryApiService {
  private readonly http = inject(HttpClient);

  getSummary(): Observable<SuggestionAnalyticsResponse> {
    return this.http.get<SuggestionAnalyticsResponse>('/api/telemetry/analytics/summary');
  }

  getBySurface(): Observable<SuggestionMetricBucket[]> {
    return this.http.get<SuggestionMetricBucket[]>('/api/telemetry/analytics/by-surface');
  }

  getBySituation(): Observable<SuggestionMetricBucket[]> {
    return this.http.get<SuggestionMetricBucket[]>('/api/telemetry/analytics/by-situation');
  }

  getByMove(): Observable<SuggestionMetricBucket[]> {
    return this.http.get<SuggestionMetricBucket[]>('/api/telemetry/analytics/by-move');
  }

  getRecentFailures(): Observable<SuggestionFailureResponse[]> {
    return this.http.get<SuggestionFailureResponse[]>('/api/telemetry/analytics/recent-failures');
  }
}
