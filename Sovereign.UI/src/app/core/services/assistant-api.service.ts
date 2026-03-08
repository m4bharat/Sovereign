import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { AiConversationDecisionRequest, AiDecisionResponse } from '../models/assistant.models';

@Injectable({ providedIn: 'root' })
export class AssistantApiService {
  private readonly http = inject(HttpClient);

  decide(payload: AiConversationDecisionRequest): Observable<AiDecisionResponse> {
    return this.http.post<AiDecisionResponse>('/api/ai/conversations/decide', payload);
  }
}
