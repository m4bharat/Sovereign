import { Injectable, inject, signal } from '@angular/core';
import { RewriteApiService } from '../../core/services/rewrite-api.service';
import { MessageRewriteVariant, RewriteMessageRequest } from '../../core/models/rewrite.models';

@Injectable()
export class RewriteFacade {
  private readonly api = inject(RewriteApiService);

  readonly loading = signal(false);
  readonly error = signal('');
  readonly variants = signal<MessageRewriteVariant[]>([]);

  submit(payload: RewriteMessageRequest): void {
    this.loading.set(true);
    this.error.set('');

    this.api.rewrite(payload).subscribe({
      next: response => {
        this.variants.set(response.variants ?? []);
        this.loading.set(false);
      },
      error: err => {
        this.error.set(err?.error?.detail || 'Failed to generate rewrites.');
        this.loading.set(false);
      }
    });
  }

  clear(): void {
    this.variants.set([]);
    this.error.set('');
  }
}
