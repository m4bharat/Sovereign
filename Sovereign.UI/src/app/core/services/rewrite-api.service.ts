import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { RewriteMessageRequest, RewriteMessageResponse } from '../models/rewrite.models';

@Injectable({ providedIn: 'root' })
export class RewriteApiService {
  private readonly http = inject(HttpClient);

  rewrite(payload: RewriteMessageRequest): Observable<RewriteMessageResponse> {
    return this.http.post<RewriteMessageResponse>('/api/ai/rewrite', payload);
  }
}
