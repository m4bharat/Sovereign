import { Injectable, computed, inject, signal } from '@angular/core';
import { RelationshipsApiService } from '../../core/services/relationships-api.service';
import { DecayAlert } from '../../core/models/relationship.models';

@Injectable()
export class DecayRadarFacade {
  private readonly api = inject(RelationshipsApiService);

  readonly loading = signal(false);
  readonly error = signal('');
  readonly alerts = signal<DecayAlert[]>([]);

  readonly reconnectCount = computed(() => this.alerts().filter(x => x.suggestedAction === 'Reconnect').length);
  readonly maintainCount = computed(() => this.alerts().filter(x => x.suggestedAction !== 'Reconnect').length);

  load(userId: string): void {
    this.loading.set(true);
    this.error.set('');

    this.api.getDecayAlerts(userId).subscribe({
      next: response => {
        this.alerts.set(response.alerts ?? []);
        this.loading.set(false);
      },
      error: err => {
        this.error.set(err?.error?.detail || 'Failed to fetch decay alerts.');
        this.loading.set(false);
      }
    });
  }

  clear(): void {
    this.alerts.set([]);
    this.error.set('');
  }
}
