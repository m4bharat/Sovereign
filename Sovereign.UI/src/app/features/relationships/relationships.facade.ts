import { Injectable, computed, inject, signal } from '@angular/core';
import { RelationshipsApiService } from '../../core/services/relationships-api.service';
import { RelationshipsStore } from '../../core/state/relationships.store';
import { CreateRelationshipRequest } from '../../core/models/relationship.models';

@Injectable()
export class RelationshipsFacade {
  private readonly api = inject(RelationshipsApiService);
  private readonly store = inject(RelationshipsStore);

  readonly items = this.store.items;
  readonly busy = signal(false);
  readonly error = signal('');
  readonly message = signal('');

  readonly hotCount = computed(() => this.items().filter(x => x.temperature === 'Hot').length);
  readonly warmCount = computed(() => this.items().filter(x => x.temperature === 'Warm').length);
  readonly coldCount = computed(() => this.items().filter(x => x.temperature === 'Cold').length);

  createRelationship(payload: CreateRelationshipRequest): void {
    this.busy.set(true);
    this.error.set('');
    this.message.set('');

    this.api.createRelationship(payload).subscribe({
      next: response => {
        this.store.add({
          relationshipId: response.relationshipId,
          userId: response.userId,
          contactId: response.contactId,
          role: response.role
        });
        this.message.set('Relationship created successfully.');
        this.busy.set(false);
        this.refreshTemperature(response.relationshipId);
      },
      error: err => {
        this.error.set(err?.error?.detail || 'Failed to create relationship.');
        this.busy.set(false);
      }
    });
  }

  refreshTemperature(relationshipId: string): void {
    this.api.getTemperature(relationshipId).subscribe({
      next: response => {
        this.store.updateTemperature(relationshipId, {
          score: response.score,
          silenceDays: response.silenceDays,
          temperature: response.temperature,
          recommendedAction: response.recommendedAction
        });
      },
      error: err => {
        this.error.set(err?.error?.detail || 'Failed to fetch temperature.');
      }
    });
  }

  refreshAll(): void {
    this.items().forEach(item => this.refreshTemperature(item.relationshipId));
  }
}
