import { Component, Input } from '@angular/core';
import { MatCardModule } from '@angular/material/card';
import { MatChipsModule } from '@angular/material/chips';

export interface CandidatePrompt {
  promptId: string;
  answer: string;
}

@Component({
  selector: 'app-discovery-card',
  standalone: true,
  imports: [MatCardModule, MatChipsModule],
  template: `
    <mat-card class="discovery-card">
      <mat-card-header>
        <mat-card-title>{{ displayName }}, {{ age }}</mat-card-title>
        <mat-card-subtitle>{{ gender }}</mat-card-subtitle>
      </mat-card-header>

      <mat-card-content>
        @if (bio) {
          <p class="bio">{{ bio }}</p>
        }

        @if (prompts && prompts.length > 0) {
          <div class="prompts-section">
            <h4>Prompts</h4>
            @for (prompt of prompts; track prompt.promptId) {
              <mat-chip class="prompt-chip">{{ prompt.answer }}</mat-chip>
            }
          </div>
        }

        @if (photoCount > 0) {
          <p class="photo-info">{{ photoCount }} photo(s)</p>
        }
      </mat-card-content>
    </mat-card>
  `,
  styles: [`
    .discovery-card { margin: 12px; max-width: 400px; }
    .bio { color: #555; margin-bottom: 12px; }
    .prompts-section { margin: 8px 0; }
    .prompt-chip { margin: 4px 4px 4px 0; }
    .photo-info { font-size: 0.85em; color: #888; margin-top: 8px; }
    h4 { margin: 0 0 8px 0; font-weight: 500; }
  `],
})
export class DiscoveryCardComponent {
  @Input({ required: true }) profileId!: string;
  @Input({ required: true }) userId!: string;
  @Input({ required: true }) displayName!: string;
  @Input() bio: string | null = null;
  @Input({ required: true }) age!: number;
  @Input({ required: true }) gender!: string;
  @Input() photoCount: number = 0;
  @Input() prompts: CandidatePrompt[] | null = null;
}
