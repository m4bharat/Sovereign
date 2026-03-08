import { Injectable, inject, signal } from '@angular/core';
import { AssistantApiService } from '../../core/services/assistant-api.service';
import { AiConversationDecisionRequest, AiDecisionResponse } from '../../core/models/assistant.models';

@Injectable()
export class AssistantFacade {
  private readonly api = inject(AssistantApiService);

  readonly loading = signal(false);
  readonly error = signal('');
  readonly decision = signal<AiDecisionResponse | null>(null);

  run(payload: AiConversationDecisionRequest): void {
    this.loading.set(true);
    this.error.set('');
    this.decision.set(null);

    this.api.decide(payload).subscribe({
      next: response => {
        this.decision.set(response);
        this.loading.set(false);
      },
      error: err => {
        this.error.set(err?.error?.detail || 'Failed to run AI decision.');
        this.loading.set(false);
      }
    });
  }

  clear(): void {
    this.decision.set(null);
    this.error.set('');
  }
}
