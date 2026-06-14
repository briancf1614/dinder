import { Component, inject, signal, OnInit } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatSelectModule } from '@angular/material/select';
import { MatInputModule } from '@angular/material/input';
import { MatIconModule } from '@angular/material/icon';
import { MatChipsModule } from '@angular/material/chips';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { ProfileService, PromptCatalogItem, PromptItem } from './profile.service';

@Component({
  selector: 'app-prompt-picker',
  standalone: true,
  imports: [
    FormsModule,
    MatCardModule, MatButtonModule, MatSelectModule,
    MatInputModule, MatIconModule, MatChipsModule, MatSnackBarModule,
  ],
  template: `
    <mat-card>
      <mat-card-header>
        <mat-card-title>Your Prompts</mat-card-title>
        <mat-card-subtitle>Select up to 3 prompts and write your answers (max 150 characters)</mat-card-subtitle>
      </mat-card-header>

      <mat-card-content>
        @if (loading()) {
          <p>Loading prompts...</p>
        } @else {
          @for (slot of selectedPrompts(); track $index; let i = $index) {
            <div class="prompt-slot">
              <mat-form-field appearance="outline" class="catalog-select">
                <mat-label>Prompt {{ i + 1 }}</mat-label>
                <mat-select
                  [ngModel]="slot.promptId"
                  (ngModelChange)="selectPrompt(i, $event)"
                >
                  @for (item of availablePrompts(); track item.id) {
                    <mat-option [value]="item.id">{{ item.text }}</mat-option>
                  }
                </mat-select>
              </mat-form-field>

              @if (slot.promptId) {
                <mat-form-field appearance="outline" class="answer-input">
                  <mat-label>Your answer</mat-label>
                  <textarea
                    matInput
                    [ngModel]="slot.answer"
                    (ngModelChange)="updateAnswer(i, $event)"
                    maxlength="150"
                    rows="2"
                  ></textarea>
                  <mat-hint align="end">{{ slot.answer.length || 0 }}/150</mat-hint>
                </mat-form-field>

                <button mat-icon-button color="warn" (click)="removeSlot(i)" aria-label="Remove prompt">
                  <mat-icon>delete</mat-icon>
                </button>
              }
            </div>
          }

          @if (selectedPrompts().length < 3) {
            <button mat-stroked-button (click)="addSlot()" class="add-btn">
              <mat-icon>add</mat-icon> Add Prompt
            </button>
          }

          @if (error()) {
            <p class="error">{{ error() }}</p>
          }
        }
      </mat-card-content>

      <mat-card-actions>
        <button mat-raised-button color="primary" (click)="save()" [disabled]="saving()">
          {{ saving() ? 'Saving...' : 'Save Prompts' }}
        </button>
      </mat-card-actions>
    </mat-card>
  `,
  styles: [`
    .prompt-slot { display: flex; gap: 12px; align-items: flex-start; margin-bottom: 16px; }
    .catalog-select { flex: 1; min-width: 200px; }
    .answer-input { flex: 2; }
    .add-btn { margin-top: 8px; }
    .error { color: #d32f2f; margin-top: 8px; }
  `],
})
export class PromptPickerComponent implements OnInit {
  private readonly profileService = inject(ProfileService);
  private readonly snackBar = inject(MatSnackBar);

  readonly loading = signal(true);
  readonly saving = signal(false);
  readonly error = signal<string | null>(null);
  readonly availablePrompts = signal<PromptCatalogItem[]>([]);
  readonly selectedPrompts = signal<{ promptId: string; answer: string }[]>([]);

  ngOnInit(): void {
    this.loadCatalog();
  }

  private loadCatalog(): void {
    this.profileService.getPromptCatalog().subscribe({
      next: (items) => {
        this.availablePrompts.set(items.filter(i => i.isEnabled));
        this.loading.set(false);
      },
      error: (err) => {
        this.error.set('Failed to load prompt catalog');
        this.loading.set(false);
      },
    });
  }

  addSlot(): void {
    if (this.selectedPrompts().length >= 3) return;
    this.selectedPrompts.update(prompts => [...prompts, { promptId: '', answer: '' }]);
  }

  removeSlot(index: number): void {
    this.selectedPrompts.update(prompts => prompts.filter((_, i) => i !== index));
  }

  selectPrompt(index: number, promptId: string): void {
    this.selectedPrompts.update(prompts =>
      prompts.map((p, i) => i === index ? { ...p, promptId } : p)
    );
  }

  updateAnswer(index: number, answer: string): void {
    this.selectedPrompts.update(prompts =>
      prompts.map((p, i) => i === index ? { ...p, answer } : p)
    );
  }

  save(): void {
    const prompts = this.selectedPrompts().filter(p => p.promptId && p.answer.trim());
    if (prompts.length === 0) {
      this.error.set('Add at least one prompt with an answer');
      return;
    }

    this.saving.set(true);
    this.error.set(null);

    this.profileService.updatePrompts({
      prompts: prompts.map(p => ({ promptId: p.promptId, answer: p.answer.trim() })),
    }).subscribe({
      next: () => {
        this.saving.set(false);
        this.snackBar.open('Prompts saved!', 'Close', { duration: 3000 });
      },
      error: (err) => {
        this.saving.set(false);
        this.error.set(err.error?.error || 'Failed to save prompts');
      },
    });
  }
}
