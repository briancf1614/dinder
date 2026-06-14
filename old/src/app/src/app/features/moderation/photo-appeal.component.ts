import { Component, inject, Input, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatInputModule } from '@angular/material/input';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { ModerationService } from './moderation.service';

@Component({
  selector: 'app-photo-appeal',
  standalone: true,
  imports: [FormsModule, MatButtonModule, MatInputModule, MatSnackBarModule],
  template: `
    <div class="appeal-section">
      @if (!showForm()) {
        <button mat-stroked-button color="accent" (click)="showForm.set(true)">
          Appeal Photo Decision
        </button>
      } @else {
        <mat-form-field appearance="outline" class="appeal-reason">
          <mat-label>Appeal reason</mat-label>
          <input matInput [ngModel]="reason()" (ngModelChange)="reason.set($event)"
                 placeholder="Why should this photo be re-reviewed?" />
        </mat-form-field>

        <div class="appeal-actions">
          <button mat-button (click)="showForm.set(false)">Cancel</button>
          <button mat-raised-button color="primary" (click)="submitAppeal()" [disabled]="submitting()">
            {{ submitting() ? 'Submitting...' : 'Submit Appeal' }}
          </button>
        </div>
      }
    </div>
  `,
  styles: [`
    .appeal-section { margin: 8px 0; }
    .appeal-reason { width: 100%; margin-bottom: 8px; }
    .appeal-actions { display: flex; gap: 8px; }
  `],
})
export class PhotoAppealComponent {
  private readonly moderationService = inject(ModerationService);
  private readonly snackBar = inject(MatSnackBar);

  @Input({ required: true }) mediaFileId!: string;

  readonly showForm = signal(false);
  readonly reason = signal('');
  readonly submitting = signal(false);

  submitAppeal(): void {
    if (!this.reason().trim()) return;

    this.submitting.set(true);
    this.moderationService.appealPhoto(this.mediaFileId, { reason: this.reason().trim() }).subscribe({
      next: (result) => {
        this.submitting.set(false);
        this.showForm.set(false);
        this.reason.set('');
        this.snackBar.open(`Appeal submitted. Status: ${result.status}`, 'Close', { duration: 3000 });
      },
      error: (err) => {
        this.submitting.set(false);
        this.snackBar.open(err.error?.error || 'Failed to submit appeal', 'Close', { duration: 3000 });
      },
    });
  }
}
