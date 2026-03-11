import { Injectable, signal } from '@angular/core';
import { LoginResponse } from '../models/auth.models';

@Injectable({ providedIn: 'root' })
export class SessionService {
  readonly token = signal(localStorage.getItem('sovereign.token') ?? '');
  readonly userId = signal(localStorage.getItem('sovereign.userId') ?? '');
  readonly email = signal(localStorage.getItem('sovereign.email') ?? '');
  readonly tenantId = signal(localStorage.getItem('sovereign.tenantId') ?? '');
  readonly isAuthenticated = signal(!!this.token());

  apply(login: LoginResponse): void {
    localStorage.setItem('sovereign.token', login.token);
    localStorage.setItem('sovereign.userId', login.userId);
    localStorage.setItem('sovereign.email', login.email);
    localStorage.setItem('sovereign.tenantId', login.tenantId);

    this.token.set(login.token);
    this.userId.set(login.userId);
    this.email.set(login.email);
    this.tenantId.set(login.tenantId);
    this.isAuthenticated.set(true);
  }

  clear(): void {
    localStorage.removeItem('sovereign.token');
    localStorage.removeItem('sovereign.userId');
    localStorage.removeItem('sovereign.email');
    localStorage.removeItem('sovereign.tenantId');
    this.token.set('');
    this.userId.set('');
    this.email.set('');
    this.tenantId.set('');
    this.isAuthenticated.set(false);
  }
}
