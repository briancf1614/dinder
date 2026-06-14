import { Component, inject, signal, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { AuthService } from '../../core/auth/auth.service';
import { DiscoveryService, CandidateDto } from './discovery.service';
import { DiscoveryCardComponent } from './discovery-card.component';

@Component({
  selector: 'app-discovery-page',
  standalone: true,
  imports: [DiscoveryCardComponent, MatButtonModule, MatIconModule, MatProgressSpinnerModule],
  template: `
    <div class="discovery-page">
      <header class="top-bar">
        <h2>Discover</h2>
        <button mat-icon-button (click)="logout()" aria-label="Logout">
          <mat-icon>logout</mat-icon>
        </button>
      </header>

      @if (loading()) {
        <div class="loading">
          <mat-spinner diameter="48" />
          <p>Finding people near you…</p>
        </div>
      } @else if (currentCandidate()) {
        <div class="card-stack">
          <app-discovery-card
            [profileId]="currentCandidate()!.profileId"
            [userId]="currentCandidate()!.userId"
            [displayName]="currentCandidate()!.displayName"
            [age]="currentCandidate()!.age"
            [gender]="currentCandidate()!.gender"
            [bio]="currentCandidate()!.bio"
            [photoCount]="currentCandidate()!.photoCount"
            [prompts]="currentCandidate()!.prompts"
          />

          <div class="actions">
            <button mat-fab class="pass-btn" (click)="swipe('Pass')" [disabled]="swiping()">
              <mat-icon>close</mat-icon>
            </button>
            <button mat-fab class="like-btn" (click)="swipe('Like')" [disabled]="swiping()">
              <mat-icon>favorite</mat-icon>
            </button>
          </div>

          @if (matchMessage()) {
            <p class="match-alert">{{ matchMessage() }}</p>
          }
        </div>
      } @else {
        <div class="empty">
          <mat-icon class="empty-icon">search_off</mat-icon>
          <p>No more profiles right now.</p>
          <p class="hint">Check back later or expand your preferences.</p>
        </div>
      }

      @if (errorMessage()) {
        <p class="error">{{ errorMessage() }}</p>
      }
    </div>
  `,
  styles: [`
    .discovery-page {
      max-width: 480px;
      margin: 0 auto;
      min-height: 100vh;
      display: flex;
      flex-direction: column;
      background: #f5f5f5;
    }
    .top-bar {
      display: flex;
      justify-content: space-between;
      align-items: center;
      padding: 16px 20px;
      background: white;
      box-shadow: 0 1px 3px rgba(0,0,0,0.08);
    }
    .top-bar h2 {
      margin: 0;
      font-weight: 600;
      color: #333;
    }
    .loading {
      flex: 1;
      display: flex;
      flex-direction: column;
      align-items: center;
      justify-content: center;
      color: #888;
      gap: 16px;
    }
    .card-stack {
      flex: 1;
      display: flex;
      flex-direction: column;
      align-items: center;
      padding: 24px 16px;
    }
    .actions {
      display: flex;
      gap: 32px;
      margin-top: 24px;
    }
    .pass-btn {
      background: white !important;
      color: #e74c3c !important;
      box-shadow: 0 4px 12px rgba(0,0,0,0.15) !important;
    }
    .like-btn {
      background: linear-gradient(135deg, #667eea, #764ba2) !important;
      color: white !important;
      box-shadow: 0 4px 12px rgba(102, 126, 234, 0.4) !important;
    }
    .match-alert {
      margin-top: 16px;
      padding: 12px 24px;
      background: #4caf50;
      color: white;
      border-radius: 12px;
      font-weight: 600;
      animation: pop 0.3s ease;
    }
    @keyframes pop {
      0% { transform: scale(0.8); opacity: 0; }
      100% { transform: scale(1); opacity: 1; }
    }
    .empty {
      flex: 1;
      display: flex;
      flex-direction: column;
      align-items: center;
      justify-content: center;
      color: #888;
      padding: 40px;
      text-align: center;
    }
    .empty-icon {
      font-size: 64px;
      width: 64px;
      height: 64px;
      margin-bottom: 16px;
      color: #bbb;
    }
    .hint {
      font-size: 0.85rem;
      color: #aaa;
    }
    .error {
      margin: 16px;
      padding: 12px;
      background: #fdecea;
      color: #e74c3c;
      border-radius: 8px;
      text-align: center;
    }
  `],
})
export class DiscoveryPageComponent implements OnInit {
  private readonly discovery = inject(DiscoveryService);
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);

  readonly loading = signal(true);
  readonly swiping = signal(false);
  readonly currentCandidate = signal<CandidateDto | null>(null);
  readonly matchMessage = signal<string | null>(null);
  readonly errorMessage = signal<string | null>(null);

  private remaining: CandidateDto[] = [];
  private nextCursor: string | null = null;

  ngOnInit(): void {
    if (!this.auth.isAuthenticated()) {
      this.router.navigate(['/login']);
      return;
    }
    this.fetchCandidates();
  }

  private fetchCandidates(): void {
    this.loading.set(true);
    this.discovery.getCandidates(undefined, undefined, this.nextCursor ?? undefined, 10).subscribe({
      next: (result) => {
        this.remaining = result.candidates;
        this.nextCursor = result.nextCursor;
        this.nextCard();
        this.loading.set(false);
      },
      error: () => {
        this.loading.set(false);
        this.errorMessage.set('Failed to load profiles. Is the backend running?');
      },
    });
  }

  private nextCard(): void {
    if (this.remaining.length > 0) {
      this.currentCandidate.set(this.remaining.shift()!);
    } else {
      this.currentCandidate.set(null);
    }
  }

  swipe(direction: 'Like' | 'Pass'): void {
    const candidate = this.currentCandidate();
    if (!candidate || this.swiping()) return;

    this.swiping.set(true);
    this.matchMessage.set(null);

    this.discovery.swipe({ targetProfileId: candidate.profileId, direction }).subscribe({
      next: (result) => {
        this.swiping.set(false);
        if (result.isMatch) {
          this.matchMessage.set('💞 It\'s a Match!');
          setTimeout(() => this.matchMessage.set(null), 3000);
        }
        this.nextCard();
        if (!this.currentCandidate() && this.nextCursor) {
          this.fetchCandidates();
        }
      },
      error: () => {
        this.swiping.set(false);
        this.errorMessage.set('Failed to record swipe.');
      },
    });
  }

  logout(): void {
    this.auth.logout();
    this.router.navigate(['/login']);
  }
}
