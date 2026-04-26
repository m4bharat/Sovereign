export interface SuggestionMetricBucket {
  name: string;
  count: number;
  acceptanceRate: number;
  averageEditRatio: number;
  regenerationRate: number;
  averageLatencyMs: number;
}

export interface SuggestionFailureResponse {
  suggestionId: string;
  userId: string;
  surface: string;
  situationType: string;
  move: string;
  latestEventType: string;
  failureReason: string;
  editRatio: number;
  regenerated: boolean;
  discarded: boolean;
  eventTime: string;
}

export interface SuggestionAnalyticsResponse {
  totalSuggestions: number;
  totalGenerated: number;
  totalInserted: number;
  totalPosted: number;
  totalDiscarded: number;
  totalRegenerated: number;
  acceptanceRate: number;
  postRate: number;
  discardRate: number;
  regenerationRate: number;
  averageEditRatio: number;
  averageLatencyMs: number;
  genericComplaintRate: number;
  wrongContextRate: number;
  wrongToneRate: number;
  hallucinationRate: number;
  bySurface: SuggestionMetricBucket[];
  bySituationType: SuggestionMetricBucket[];
  byMove: SuggestionMetricBucket[];
}
