import { Component, inject } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { AuthService } from '../../core/auth/auth.service';

@Component({
  selector: 'app-register-page',
  standalone: true,
  imports: [FormsModule, RouterLink],
  template: `
    <div class="auth-container">
      <div class="auth-card">
        <h1>Join Dinder</h1>
        <p class="subtitle">Create your account</p>

        <form (ngSubmit)="onSubmit()" #registerForm="ngForm">
          <div class="form-group">
            <label for="email">Email</label>
            <input
              id="email"
              type="email"
              name="email"
              [(ngModel)]="email"
              required
              placeholder="you@example.com"
              autocomplete="email"
            />
          </div>

          <div class="form-group">
            <label for="password">Password</label>
            <input
              id="password"
              type="password"
              name="password"
              [(ngModel)]="password"
              required
              minlength="6"
              placeholder="At least 6 characters"
              autocomplete="new-password"
            />
          </div>

          <div class="form-group">
            <label for="confirmPassword">Confirm Password</label>
            <input
              id="confirmPassword"
              type="password"
              name="confirmPassword"
              [(ngModel)]="confirmPassword"
              required
              placeholder="Repeat your password"
              autocomplete="new-password"
            />
          </div>

          @if (errorMessage) {
            <p class="error">{{ errorMessage }}</p>
          }

          <button type="submit" [disabled]="loading">
            {{ loading ? 'Creating account…' : 'Create Account' }}
          </button>
        </form>

        <p class="switch-link">
          Already have an account?
          <a routerLink="/login">Sign In</a>
        </p>
      </div>
    </div>
  `,
  styles: [`
    .auth-container {
      display: flex;
      justify-content: center;
      align-items: center;
      min-height: 100vh;
      background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
    }
    .auth-card {
      background: white;
      border-radius: 16px;
      padding: 40px;
      width: 100%;
      max-width: 400px;
      box-shadow: 0 20px 60px rgba(0,0,0,0.3);
    }
    h1 {
      margin: 0 0 4px;
      font-size: 2rem;
      color: #333;
      text-align: center;
    }
    .subtitle {
      text-align: center;
      color: #888;
      margin-bottom: 32px;
    }
    .form-group {
      margin-bottom: 20px;
    }
    label {
      display: block;
      margin-bottom: 6px;
      font-weight: 500;
      color: #444;
    }
    input {
      width: 100%;
      padding: 12px 16px;
      border: 2px solid #e0e0e0;
      border-radius: 10px;
      font-size: 1rem;
      outline: none;
      transition: border-color 0.2s;
      box-sizing: border-box;
    }
    input:focus {
      border-color: #667eea;
    }
    button {
      width: 100%;
      padding: 14px;
      background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
      color: white;
      border: none;
      border-radius: 10px;
      font-size: 1.1rem;
      font-weight: 600;
      cursor: pointer;
      margin-top: 8px;
      transition: opacity 0.2s;
    }
    button:disabled {
      opacity: 0.6;
      cursor: not-allowed;
    }
    .error {
      color: #e74c3c;
      background: #fdecea;
      padding: 10px 14px;
      border-radius: 8px;
      font-size: 0.9rem;
      margin: 12px 0 0;
    }
    .switch-link {
      text-align: center;
      margin-top: 20px;
      color: #888;
      font-size: 0.9rem;
    }
    .switch-link a {
      color: #667eea;
      text-decoration: none;
      font-weight: 600;
    }
  `],
})
export class RegisterPageComponent {
  email = '';
  password = '';
  confirmPassword = '';
  loading = false;
  errorMessage: string | null = null;

  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);

  onSubmit(): void {
    if (!this.email || !this.password || !this.confirmPassword) return;

    if (this.password !== this.confirmPassword) {
      this.errorMessage = 'Passwords do not match.';
      return;
    }

    this.loading = true;
    this.errorMessage = null;

    this.auth.register({ email: this.email, password: this.password }).subscribe({
      next: () => this.router.navigate(['/discovery']),
      error: (err) => {
        this.loading = false;
        if (err.status === 409) {
          this.errorMessage = 'An account with this email already exists.';
        } else if (err.error?.detail) {
          this.errorMessage = err.error.detail;
        } else {
          this.errorMessage = 'Connection error. Is the backend running?';
        }
      },
    });
  }
}
