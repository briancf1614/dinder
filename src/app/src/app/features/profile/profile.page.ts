import { Component, inject, signal, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { AuthService } from '../../core/auth/auth.service';
import { ProfileService, Profile } from './profile.service';
import { PromptPickerComponent } from './prompt-picker.component';

@Component({
  selector: 'app-profile-page',
  standalone: true,
  imports: [FormsModule, MatButtonModule, MatIconModule, MatProgressSpinnerModule, PromptPickerComponent],
  template: `
    <div class="profile-page">
      <header class="top-bar">
        <button mat-icon-button (click)="router.navigate(['/discovery'])" aria-label="Back">
          <mat-icon>arrow_back</mat-icon>
        </button>
        <h2>Profile</h2>
        <button mat-icon-button (click)="logout()" aria-label="Logout">
          <mat-icon>logout</mat-icon>
        </button>
      </header>

      @if (loading()) {
        <div class="loading">
          <mat-spinner diameter="48" />
        </div>
      } @else if (profile()) {
        <div class="content">
          <form (ngSubmit)="save()">
            <div class="form-group">
              <label for="displayName">Display Name</label>
              <input
                id="displayName"
                type="text"
                [(ngModel)]="editName"
                name="displayName"
                required
                placeholder="Your name"
              />
            </div>

            <div class="form-group">
              <label>Gender</label>
              <div class="gender-toggle">
                <button
                  type="button"
                  [class.active]="editGender === 'Man'"
                  (click)="editGender = 'Man'"
                >Man</button>
                <button
                  type="button"
                  [class.active]="editGender === 'Woman'"
                  (click)="editGender = 'Woman'"
                >Woman</button>
                <button
                  type="button"
                  [class.active]="editGender === 'NonBinary'"
                  (click)="editGender = 'NonBinary'"
                >Non-binary</button>
              </div>
            </div>

            <div class="form-group">
              <label for="bio">Bio</label>
              <textarea
                id="bio"
                [(ngModel)]="editBio"
                name="bio"
                rows="4"
                placeholder="Tell people about yourself…"
              ></textarea>
            </div>

            <div class="form-group">
              <label>Prompts</label>
              <app-prompt-picker />
            </div>

            @if (savedMessage()) {
              <p class="saved">{{ savedMessage() }}</p>
            }

            <button type="submit" [disabled]="saving()" class="save-btn">
              {{ saving() ? 'Saving…' : 'Save Changes' }}
            </button>
          </form>
        </div>
      } @else {
        <div class="empty">
          <p>Could not load profile.</p>
          <button mat-stroked-button (click)="fetchProfile()">Retry</button>
        </div>
      }
    </div>
  `,
  styles: [`
    .profile-page {
      max-width: 480px;
      margin: 0 auto;
      min-height: 100vh;
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
    .top-bar h2 { margin: 0; font-weight: 600; color: #333; }
    .loading {
      display: flex;
      justify-content: center;
      padding: 60px;
    }
    .content {
      padding: 24px 20px;
    }
    .form-group {
      margin-bottom: 24px;
    }
    label {
      display: block;
      margin-bottom: 6px;
      font-weight: 500;
      color: #444;
    }
    input, textarea {
      width: 100%;
      padding: 12px 16px;
      border: 2px solid #e0e0e0;
      border-radius: 10px;
      font-size: 1rem;
      outline: none;
      font-family: inherit;
      box-sizing: border-box;
    }
    input:focus, textarea:focus {
      border-color: #667eea;
    }
    textarea { resize: vertical; }
    .gender-toggle {
      display: flex;
      gap: 8px;
    }
    .gender-toggle button {
      flex: 1;
      padding: 10px;
      border: 2px solid #e0e0e0;
      border-radius: 10px;
      background: white;
      cursor: pointer;
      font-size: 0.9rem;
      transition: all 0.2s;
    }
    .gender-toggle button.active {
      border-color: #667eea;
      background: #667eea;
      color: white;
    }
    .save-btn {
      width: 100%;
      padding: 14px;
      background: linear-gradient(135deg, #667eea, #764ba2);
      color: white;
      border: none;
      border-radius: 10px;
      font-size: 1.1rem;
      font-weight: 600;
      cursor: pointer;
      margin-top: 8px;
    }
    .save-btn:disabled { opacity: 0.6; cursor: not-allowed; }
    .saved {
      padding: 10px 14px;
      background: #e8f5e9;
      color: #2e7d32;
      border-radius: 8px;
      font-size: 0.9rem;
      margin: 12px 0;
      text-align: center;
    }
    .empty {
      padding: 60px;
      text-align: center;
      color: #888;
    }
  `],
})
export class ProfilePageComponent implements OnInit {
  private readonly profileSvc = inject(ProfileService);
  private readonly auth = inject(AuthService);
  readonly router = inject(Router);

  readonly loading = signal(true);
  readonly saving = signal(false);
  readonly profile = signal<Profile | null>(null);
  readonly savedMessage = signal<string | null>(null);

  editName = '';
  editGender = '';
  editBio = '';
  editPrompts: { promptId: string; answer: string }[] = [];

  ngOnInit(): void {
    if (!this.auth.isAuthenticated()) {
      this.router.navigate(['/login']);
      return;
    }
    this.fetchProfile();
  }

  fetchProfile(): void {
    this.loading.set(true);
    this.profileSvc.getProfile().subscribe({
      next: (p) => {
        this.profile.set(p);
        this.editName = p.displayName;
        this.editGender = p.gender;
        this.editBio = p.bio ?? '';
        this.editPrompts = p.prompts ?? [];
        this.loading.set(false);
      },
      error: () => {
        this.loading.set(false);
      },
    });
  }

  save(): void {
    this.saving.set(true);
    this.savedMessage.set(null);

    this.profileSvc.updateProfile({
      displayName: this.editName,
      gender: this.editGender,
      bio: this.editBio || undefined,
      prompts: this.editPrompts,
    }).subscribe({
      next: (p) => {
        this.profile.set(p);
        this.saving.set(false);
        this.savedMessage.set('Profile saved!');
        setTimeout(() => this.savedMessage.set(null), 3000);
      },
      error: () => {
        this.saving.set(false);
      },
    });
  }

  logout(): void {
    this.auth.logout();
    this.router.navigate(['/login']);
  }
}
