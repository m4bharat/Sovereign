export interface CreateRelationshipRequest {
  userId: string;
  contactId: string;
  role: string;
}

export interface CreateRelationshipResponse {
  relationshipId: string;
  userId: string;
  contactId: string;
  role: string;
}

export interface RelationshipTemperatureResponse {
  relationshipId: string;
  score: number;
  silenceDays: number;
  temperature: string;
  recommendedAction: string;
}

export interface DecayAlert {
  relationshipId: string;
  contactId: string;
  daysSilent: number;
  decayScore: number;
  temperature: string;
  suggestedAction: string;
  suggestedMessage: string;
}

export interface DecayAlertsResponse {
  alerts: DecayAlert[];
}

export interface RelationshipCardVm {
  relationshipId: string;
  userId: string;
  contactId: string;
  role: string;
  score?: number;
  silenceDays?: number;
  temperature?: string;
  recommendedAction?: string;
}
