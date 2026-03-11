import { Component, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { AuthApiService } from '../../core/services/auth-api.service';
import { SessionService } from '../../core/services/session.service';

@Component({
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './auth-page.component.html'
})
export class AuthPageComponent {
  private readonly api = inject(AuthApiService);
  readonly session = inject(SessionService);

  readonly email = signal('founder@sovereign.ai');
  readonly password = signal('ChangeMe123!');
  readonly tenantId = signal('sovereign-dev');
  readonly message = signal('');
  readonly loading = signal(false);

  register(): void {
    this.loading.set(true);
    this.api.register({
      email: this.email(),
      password: this.password(),
      tenantId: this.tenantId()
    }).subscribe({
      next: response => {
        this.session.apply(response);
        this.message.set('Registered and signed in.');
        this.loading.set(false);
      },
      error: error => {
        this.message.set(error?.error?.message || 'Registration failed.');
        this.loading.set(false);
      }
    });
  }

  login(): void {
    this.loading.set(true);
    this.api.login({
      email: this.email(),
      password: this.password(),
      tenantId: this.tenantId()
    }).subscribe({
      next: response => {
        this.session.apply(response);
        this.message.set('Logged in successfully.');
        this.loading.set(false);
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
}
