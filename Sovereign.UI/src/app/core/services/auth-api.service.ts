import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { AuthMeResponse, LoginRequest, LoginResponse, RegisterRequest } from '../models/auth.models';

@Injectable({ providedIn: 'root' })
export class AuthApiService {
  private readonly http = inject(HttpClient);

  register(payload: RegisterRequest): Observable<LoginResponse> {
    return this.http.post<LoginResponse>('/api/auth/register', payload);
  }

  login(payload: LoginRequest): Observable<LoginResponse> {
    return this.http.post<LoginResponse>('/api/auth/login', payload);
  }

  me(): Observable<AuthMeResponse> {
    return this.http.get<AuthMeResponse>('/api/auth/me');
  }
}
