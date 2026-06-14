import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatSelectModule } from '@angular/material/select';
import { MatInputModule } from '@angular/material/input';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { ModerationService } from './moderation.service';

interface ReasonOption {
  value: string;
  label: string;
  subCategories: { value: string; label: string }[];
}

@Component({
  selector: 'app-report-form',
  standalone: true,
  imports: [
    FormsModule,
    MatCardModule, MatButtonModule, MatSelectModule,
    MatInputModule, MatSnackBarModule,
  ],
  template: `
    <mat-card>
      <mat-card-header>
        <mat-card-title>Report User</mat-card-title>
      </mat-card-header>

      <mat-card-content>
        <mat-form-field appearance="outline" class="full-width">
          <mat-label>Reported User ID</mat-label>
          <input matInput [ngModel]="reportedUserId()" (ngModelChange)="reportedUserId.set($event)" />
        </mat-form-field>

        <mat-form-field appearance="outline" class="full-width">
          <mat-label>Reason</mat-label>
          <mat-select [ngModel]="selectedReason()" (ngModelChange)="onReasonChange($event)">
            @for (r of reasons; track r.value) {
              <mat-option [value]="r.value">{{ r.label }}</mat-option>
            }
          </mat-select>
        </mat-form-field>

        @if (currentSubCategories().length > 0) {
          <mat-form-field appearance="outline" class="full-width">
            <mat-label>Sub-Category</mat-label>
            <mat-select [ngModel]="selectedSubCategory()" (ngModelChange)="selectedSubCategory.set($event)">
              <mat-option [value]="">None</mat-option>
              @for (sc of currentSubCategories(); track sc.value) {
                <mat-option [value]="sc.value">{{ sc.label }}</mat-option>
              }
            </mat-select>
          </mat-form-field>
        }

        <mat-form-field appearance="outline" class="full-width">
          <mat-label>Description (optional)</mat-label>
          <textarea matInput [ngModel]="description()" (ngModelChange)="description.set($event)" rows="3"></textarea>
        </mat-form-field>

        @if (error()) {
          <p class="error">{{ error() }}</p>
        }
      </mat-card-content>

      <mat-card-actions>
        <button mat-raised-button color="warn" (click)="submit()" [disabled]="submitting()">
          {{ submitting() ? 'Submitting...' : 'Submit Report' }}
        </button>
      </mat-card-actions>
    </mat-card>
  `,
  styles: [`
    .full-width { width: 100%; margin-bottom: 12px; }
    .error { color: #d32f2f; }
  `],
})
export class ReportFormComponent {
  private readonly moderationService = inject(ModerationService);
  private readonly snackBar = inject(MatSnackBar);

  readonly reportedUserId = signal('');
  readonly selectedReason = signal('');
  readonly selectedSubCategory = signal('');
  readonly description = signal('');
  readonly submitting = signal(false);
  readonly error = signal<string | null>(null);

  readonly reasons: ReasonOption[] = [
    {
      value: 'Harassment', label: 'Harassment',
      subCategories: [
        { value: 'VerbalAbuse', label: 'Verbal Abuse' },
        { value: 'PhysicalThreat', label: 'Physical Threat' },
        { value: 'Stalking', label: 'Stalking' },
      ],
    },
    {
      value: 'FakeProfile', label: 'Fake Profile',
      subCategories: [
        { value: 'Catfish', label: 'Catfish' },
        { value: 'Scam', label: 'Scam' },
        { value: 'Bot', label: 'Bot' },
      ],
    },
    {
      value: 'InappropriatePhotos', label: 'Inappropriate Photos',
      subCategories: [
        { value: 'Nudity', label: 'Nudity' },
        { value: 'Violence', label: 'Violence' },
        { value: 'SpamImage', label: 'Spam Image' },
      ],
    },
    { value: 'Spam', label: 'Spam', subCategories: [] },
    { value: 'Other', label: 'Other', subCategories: [] },
  ];

  currentSubCategories = signal<{ value: string; label: string }[]>([]);

  onReasonChange(reason: string): void {
    this.selectedReason.set(reason);
    this.selectedSubCategory.set('');

    const found = this.reasons.find(r => r.value === reason);
    this.currentSubCategories.set(found?.subCategories ?? []);
  }

  submit(): void {
    const userId = this.reportedUserId().trim();
    const reason = this.selectedReason();

    if (!userId || !reason) {
      this.error.set('User ID and reason are required');
      return;
    }

    this.submitting.set(true);
    this.error.set(null);

    this.moderationService.reportUser({
      reportedUserId: userId,
      reason,
      subCategory: this.selectedSubCategory() || undefined,
      description: this.description() || undefined,
    }).subscribe({
      next: (result) => {
        this.submitting.set(false);
        this.snackBar.open(
          result.isDuplicate ? 'Report submitted (duplicate)' : 'Report submitted',
          'Close',
          { duration: 3000 },
        );
        // Reset form
        this.reportedUserId.set('');
        this.selectedReason.set('');
        this.selectedSubCategory.set('');
        this.description.set('');
        this.currentSubCategories.set([]);
      },
      error: (err) => {
        this.submitting.set(false);
        this.error.set(err.error?.error || 'Failed to submit report');
      },
    });
  }
}
