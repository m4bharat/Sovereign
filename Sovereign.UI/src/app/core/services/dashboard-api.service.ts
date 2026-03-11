import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { DashboardOverviewResponse } from '../models/dashboard.models';

@Injectable({ providedIn: 'root' })
export class DashboardApiService {
  private readonly http = inject(HttpClient);

  getOverview(userId: string): Observable<DashboardOverviewResponse> {
    return this.http.get<DashboardOverviewResponse>(`/api/dashboard/${encodeURIComponent(userId)}`);
  }
}
