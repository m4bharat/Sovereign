import { DecayAlert } from './relationship.models';

export interface MemoryEntryDto {
  id: string;
  key: string;
  value: string;
  createdAtUtc: string;
}

export interface DashboardOverviewResponse {
  relationshipCount: number;
  hotRelationships: number;
  warmRelationships: number;
  coldRelationships: number;
  openDecayAlerts: number;
  memoryCount: number;
  priorityAlerts: DecayAlert[];
  recentMemories: MemoryEntryDto[];
}
