import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import {
  CreateRelationshipRequest,
  CreateRelationshipResponse,
  RelationshipTemperatureResponse,
  DecayAlertsResponse
} from '../models/relationship.models';

@Injectable({ providedIn: 'root' })
export class RelationshipsApiService {
  private readonly http = inject(HttpClient);

  createRelationship(payload: CreateRelationshipRequest): Observable<CreateRelationshipResponse> {
    return this.http.post<CreateRelationshipResponse>('/api/relationships', payload);
  }

  getTemperature(relationshipId: string): Observable<RelationshipTemperatureResponse> {
    return this.http.get<RelationshipTemperatureResponse>(`/api/relationships/${relationshipId}/temperature`);
  }

  getDecayAlerts(userId: string): Observable<DecayAlertsResponse> {
    return this.http.get<DecayAlertsResponse>(`/api/relationships/decay-alerts?userId=${encodeURIComponent(userId)}`);
  }
}
