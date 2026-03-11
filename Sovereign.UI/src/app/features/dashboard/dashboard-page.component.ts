import { Component, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { DashboardApiService } from '../../core/services/dashboard-api.service';
import { SessionService } from '../../core/services/session.service';
import { DashboardOverviewResponse } from '../../core/models/dashboard.models';

@Component({
  standalone: true,
  imports: [CommonModule],
  templateUrl: './dashboard-page.component.html'
})
export class DashboardPageComponent {
  private readonly api = inject(DashboardApiService);
  readonly session = inject(SessionService);

  readonly loading = signal(false);
  readonly error = signal('');
  readonly overview = signal<DashboardOverviewResponse | null>(null);

  load(): void {
    const userId = this.session.userId();
    if (!userId) {
      this.error.set('Log in first to load the dashboard.');
      return;
    }

    this.loading.set(true);
    this.error.set('');

    this.api.getOverview(userId).subscribe({
      next: response => {
        this.overview.set(response);
        this.loading.set(false);
      },
      error: error => {
        this.error.set(error?.error?.message || 'Failed to load dashboard.');
        this.loading.set(false);
      }
    });
  }
}
