import { Injectable, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, tap } from 'rxjs';

export interface AuthTokens {
  userId: string;
  accessToken: string;
  refreshToken: string;
}

export interface RegisterRequest {
  email: string;
  password: string;
}

export interface LoginRequest {
  email: string;
  password: string;
}

export interface ExternalLoginRequest {
  email: string;
  provider: 'Google' | 'Apple';
  providerUserId: string;
}

const TOKENS_KEY = 'dinder_tokens';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly apiBase = '/api/v1/identity';
  readonly isAuthenticated = signal(false);
  readonly currentUserId = signal<string | null>(null);

  constructor(private http: HttpClient) {
    this.loadStoredTokens();
  }

  register(request: RegisterRequest): Observable<AuthTokens> {
    return this.http.post<AuthTokens>(`${this.apiBase}/register`, request).pipe(
      tap(tokens => this.storeTokens(tokens))
    );
  }

  login(request: LoginRequest): Observable<AuthTokens> {
    return this.http.post<AuthTokens>(`${this.apiBase}/login`, request).pipe(
      tap(tokens => this.storeTokens(tokens))
    );
  }

  externalLogin(request: ExternalLoginRequest): Observable<AuthTokens> {
    return this.http.post<AuthTokens>(`${this.apiBase}/login/external`, request).pipe(
      tap(tokens => this.storeTokens(tokens))
    );
  }

  refreshToken(): Observable<{ accessToken: string; refreshToken: string }> {
    const tokens = this.getStoredTokens();
    return this.http.post<{ accessToken: string; refreshToken: string }>(
      `${this.apiBase}/refresh`,
      { refreshToken: tokens?.refreshToken }
    ).pipe(
      tap(result => {
        if (tokens) {
          tokens.accessToken = result.accessToken;
          tokens.refreshToken = result.refreshToken;
          localStorage.setItem(TOKENS_KEY, JSON.stringify(tokens));
        }
      })
    );
  }

  deleteAccount(): Observable<void> {
    return this.http.delete<void>(`${this.apiBase}/account`).pipe(
      tap(() => this.clearTokens())
    );
  }

  getAccessToken(): string | null {
    return this.getStoredTokens()?.accessToken ?? null;
  }

  logout(): void {
    this.clearTokens();
  }

  private storeTokens(tokens: AuthTokens): void {
    localStorage.setItem(TOKENS_KEY, JSON.stringify(tokens));
    this.isAuthenticated.set(true);
    this.currentUserId.set(tokens.userId);
  }

  private loadStoredTokens(): void {
    const tokens = this.getStoredTokens();
    if (tokens) {
      this.isAuthenticated.set(true);
      this.currentUserId.set(tokens.userId);
    }
  }

  private getStoredTokens(): AuthTokens | null {
    const raw = localStorage.getItem(TOKENS_KEY);
    if (!raw) return null;
    try {
      return JSON.parse(raw) as AuthTokens;
    } catch {
      return null;
    }
  }

  private clearTokens(): void {
    localStorage.removeItem(TOKENS_KEY);
    this.isAuthenticated.set(false);
    this.currentUserId.set(null);
  }
}
