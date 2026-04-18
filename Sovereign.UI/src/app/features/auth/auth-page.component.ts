import { Component, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { AuthApiService } from '../../core/services/auth-api.service';
import { SessionService } from '../../core/services/session.service';

@Component({
  standalone: true,
  imports: [CommonModule, FormsModule],
  template: `
    <div class="auth-page">
      <div class="card">
        <h1>Sovereign</h1>
        <p class="subtext">Sign in to use Suggest with Sovereign.</p>

        <label>
          <span>Email</span>
          <input [ngModel]="email()" (ngModelChange)="email.set($event)" type="email" autocomplete="email" />
        </label>

        <label>
          <span>Password</span>
          <input [ngModel]="password()" (ngModelChange)="password.set($event)" type="password" autocomplete="current-password" />
        </label>

        <label>
          <span>Tenant</span>
          <input [ngModel]="tenantId()" (ngModelChange)="tenantId.set($event)" type="text" />
        </label>

        <div class="actions">
          <button type="button" [disabled]="loading()" (click)="login()">{{ loading() ? 'Please wait…' : 'Login' }}</button>
          <button type="button" class="secondary" [disabled]="loading()" (click)="register()">Register</button>
        </div>

        <p class="message" *ngIf="message()">{{ message() }}</p>

        <div class="session" *ngIf="session.isAuthenticated()">
          <p><strong>Signed in as:</strong> {{ session.email() }}</p>
          <p><strong>Tenant:</strong> {{ session.tenantId() }}</p>
          <div class="actions">
            <button type="button" (click)="continueToDashboard()">Continue</button>
            <button type="button" class="secondary" (click)="logout()">Logout</button>
          </div>
        </div>
      </div>
    </div>
  `,
  styles: [
    `
      .auth-page { min-height: 100vh; display: grid; place-items: center; background: #f4f7fb; padding: 24px; }
      .card { width: 100%; max-width: 420px; background: #fff; border-radius: 16px; padding: 24px; box-shadow: 0 8px 30px rgba(15, 23, 42, 0.08); }
      h1 { margin: 0 0 8px; font-size: 28px; }
      .subtext { margin: 0 0 20px; color: #516074; }
      label { display: grid; gap: 6px; margin-bottom: 14px; }
      span { font-size: 13px; font-weight: 600; color: #253243; }
      input { border: 1px solid #d6deea; border-radius: 10px; padding: 10px 12px; font-size: 14px; }
      .actions { display: flex; gap: 10px; margin-top: 8px; flex-wrap: wrap; }
      button { border: none; border-radius: 999px; padding: 10px 16px; font-weight: 700; cursor: pointer; background: #0a66c2; color: #fff; }
      button.secondary { background: #e8eef7; color: #1f2e43; }
      button:disabled { opacity: 0.72; cursor: wait; }
      .message { margin-top: 14px; color: #1f2e43; }
      .session { margin-top: 18px; border-top: 1px solid #eef2f7; padding-top: 16px; }
      .session p { margin: 6px 0; }
    `
  ]
})
export class AuthPageComponent {
  private readonly api = inject(AuthApiService);
  private readonly router = inject(Router);
  readonly session = inject(SessionService);

  readonly email = signal('founder@sovereign.ai');
  readonly password = signal('ChangeMe123!');
  readonly tenantId = signal('sovereign-dev');
  readonly message = signal('');
  readonly loading = signal(false);

  register(): void {
    this.loading.set(true);
    this.message.set('');

    this.api.register({
      email: this.email(),
      password: this.password(),
      tenantId: this.tenantId()
    }).subscribe({
      next: response => {
        this.session.apply(response);
        this.message.set('Registered and signed in.');
        this.loading.set(false);
        void this.router.navigateByUrl('/dashboard');
      },
      error: error => {
        this.message.set(error?.error?.message || 'Registration failed.');
        this.loading.set(false);
      }
    });
  }

  login(): void {
    this.loading.set(true);
    this.message.set('');

    this.api.login({
      email: this.email(),
      password: this.password(),
      tenantId: this.tenantId()
    }).subscribe({
      next: response => {
        this.session.apply(response);
        this.message.set('Logged in successfully.');
        this.loading.set(false);
        void this.router.navigateByUrl('/dashboard');
      },
      error: error => {
        this.message.set(error?.error?.message || 'Login failed.');
        this.loading.set(false);
      }
    });
  }

  logout(): void {
    this.session.clear();
    this.message.set('Signed out.');
  }

  continueToDashboard(): void {
    void this.router.navigateByUrl('/dashboard');
  }
}
