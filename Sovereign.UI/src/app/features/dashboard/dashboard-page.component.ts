import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';
import { SessionService } from '../../core/services/session.service';

@Component({
  standalone: true,
  imports: [CommonModule],
  template: `
    <div class="dashboard-page">
      <div class="card">
        <h1>Session ready</h1>
        <p class="subtext">Your account is authenticated. You can go back to LinkedIn and use Suggest with Sovereign.</p>

        <div class="meta">
          <p><strong>Email:</strong> {{ session.email() || '—' }}</p>
          <p><strong>Tenant:</strong> {{ session.tenantId() || '—' }}</p>
          <p><strong>Status:</strong> {{ session.isAuthenticated() ? 'Authenticated' : 'Signed out' }}</p>
        </div>

        <div class="actions">
          <button type="button" (click)="logout()">Logout</button>
          <button type="button" class="secondary" (click)="goToAuth()">Back to auth</button>
        </div>
      </div>
    </div>
  `,
  styles: [
    `
      .dashboard-page { min-height: 100vh; display: grid; place-items: center; background: #f4f7fb; padding: 24px; }
      .card { width: 100%; max-width: 560px; background: #fff; border-radius: 16px; padding: 24px; box-shadow: 0 8px 30px rgba(15, 23, 42, 0.08); }
      h1 { margin: 0 0 8px; font-size: 28px; }
      .subtext { margin: 0 0 20px; color: #516074; }
      .meta { display: grid; gap: 8px; margin: 20px 0; }
      .meta p { margin: 0; }
      .actions { display: flex; gap: 10px; flex-wrap: wrap; }
      button { border: none; border-radius: 999px; padding: 10px 16px; font-weight: 700; cursor: pointer; background: #0a66c2; color: #fff; }
      button.secondary { background: #e8eef7; color: #1f2e43; }
    `
  ]
})
export class DashboardPageComponent {
  private readonly router = inject(Router);
  readonly session = inject(SessionService);

  logout(): void {
    this.session.clear();
    void this.router.navigateByUrl('/auth');
  }

  goToAuth(): void {
    void this.router.navigateByUrl('/auth');
  }
}
