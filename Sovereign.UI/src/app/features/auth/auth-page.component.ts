import { Component, computed, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { AuthApiService } from '../../core/services/auth-api.service';
import { SessionService } from '../../core/services/session.service';
import { firstValueFrom } from 'rxjs';

@Component({
  selector: 'app-auth-page',
  standalone: true,
  imports: [CommonModule, FormsModule],
  template: `
    <section class="auth-page">
      <div class="auth-card">
        <h1>Sign in to use Suggest with Sovereign.</h1>

        <ng-container *ngIf="!session.isAuthenticated(); else signedInBlock">
          <label class="field">
            <span>Email</span>
            <input
              type="email"
              [(ngModel)]="email"
              placeholder="founder@sovereign.ai"
              autocomplete="email"
            />
          </label>

          <label class="field">
            <span>Password</span>
            <input
              type="password"
              [(ngModel)]="password"
              placeholder="••••••••••"
              autocomplete="current-password"
            />
          </label>

          <label class="field">
            <span>Tenant</span>
            <input
              type="text"
              [(ngModel)]="tenantId"
              placeholder="sovereign-dev"
              autocomplete="organization"
            />
          </label>

          <div class="actions">
            <button type="button" (click)="onLogin()" [disabled]="busy()">
              {{ busy() ? 'Working...' : 'Login' }}
            </button>

            <button type="button" (click)="onRegister()" [disabled]="busy()">
              {{ busy() ? 'Working...' : 'Register' }}
            </button>
          </div>

          <p class="status error" *ngIf="error()">{{ error() }}</p>
          <p class="status success" *ngIf="success()">{{ success() }}</p>
        </ng-container>

        <ng-template #signedInBlock>
          <div class="signed-in-block">
            <p><strong>Signed in as:</strong> {{ session.email() }}</p>
            <p><strong>Tenant:</strong> {{ session.tenantId() }}</p>

            <div class="actions">
              <button type="button" (click)="onContinue()" [disabled]="busy()">
                Continue
              </button>

              <button type="button" (click)="onLogout()" [disabled]="busy()">
                Logout
              </button>
            </div>

            <p class="status error" *ngIf="error()">{{ error() }}</p>
            <p class="status success" *ngIf="success()">{{ success() }}</p>
          </div>
        </ng-template>
      </div>
    </section>
  `,
  styles: [`
    .auth-page {
      min-height: 100vh;
      display: grid;
      place-items: center;
      padding: 24px;
      background: #f6f8fb;
    }

    .auth-card {
      width: 100%;
      max-width: 420px;
      background: #fff;
      border-radius: 16px;
      padding: 24px;
      box-shadow: 0 10px 30px rgba(0,0,0,0.08);
    }

    h1 {
      font-size: 20px;
      line-height: 1.3;
      margin: 0 0 20px;
    }

    .field {
      display: block;
      margin-bottom: 14px;
    }

    .field span {
      display: block;
      font-size: 13px;
      font-weight: 600;
      margin-bottom: 6px;
      color: #334155;
    }

    .field input {
      width: 100%;
      border: 1px solid #d0d7e2;
      border-radius: 10px;
      padding: 10px 12px;
      font-size: 14px;
      box-sizing: border-box;
    }

    .actions {
      display: flex;
      gap: 10px;
      margin-top: 16px;
      flex-wrap: wrap;
    }

    button {
      border: none;
      border-radius: 10px;
      padding: 10px 14px;
      background: #0a66c2;
      color: white;
      font-weight: 600;
      cursor: pointer;
    }

    button[disabled] {
      opacity: 0.7;
      cursor: wait;
    }

    .status {
      margin-top: 14px;
      font-size: 13px;
    }

    .status.error {
      color: #b00020;
    }

    .status.success {
      color: #1a7f37;
    }

    .signed-in-block p {
      margin: 8px 0;
    }
  `]
})
export class AuthPageComponent {
  private readonly authApi = inject(AuthApiService);
  readonly session = inject(SessionService);
  private readonly router = inject(Router);

  email = 'founder@sovereign.ai';
  password = '';
  tenantId = 'sovereign-dev';

  readonly busy = signal(false);
  readonly error = signal('');
  readonly success = signal('');

  async onLogin(): Promise<void> {
    this.error.set('');
    this.success.set('');

    if (!this.email.trim() || !this.password.trim() || !this.tenantId.trim()) {
      this.error.set('Email, password, and tenant are required.');
      return;
    }

    this.busy.set(true);

    try {
            const response = await firstValueFrom(
              this.authApi.login({
                email: this.email.trim(),
                password: this.password.trim(),
                tenantId: this.tenantId.trim()
              })
            );

      this.session.apply(response);
      this.success.set('Login successful. You can continue back to LinkedIn.');
    } catch (err: any) {
      this.error.set(err?.message || 'Login failed.');
    } finally {
      this.busy.set(false);
    }
  }

  async onRegister(): Promise<void> {
    this.error.set('');
    this.success.set('');

    if (!this.email.trim() || !this.password.trim() || !this.tenantId.trim()) {
      this.error.set('Email, password, and tenant are required.');
      return;
    }

    this.busy.set(true);

    try {
      const response = await firstValueFrom(
            this.authApi.register({
              email: this.email.trim(),
              password: this.password.trim(),
              tenantId: this.tenantId.trim()
            })
          );

      this.session.apply(response);
      this.success.set('Registration successful. You can continue back to LinkedIn.');
    } catch (err: any) {
      this.error.set(err?.message || 'Registration failed.');
    } finally {
      this.busy.set(false);
    }
  }

  async onContinue(): Promise<void> {
    this.error.set('');
    this.success.set('');

    this.busy.set(true);

    try {
      await this.session.continueToLinkedInAndResume();
    } catch (err: any) {
      this.error.set(err?.message || 'Could not return to LinkedIn.');
    } finally {
      this.busy.set(false);
    }
  }

  onLogout(): void {
    this.session.clear();
    this.success.set('');
    this.error.set('');
  }
}