import { Component, Input } from '@angular/core';
import { MatCardModule } from '@angular/material/card';
import { MatIconModule } from '@angular/material/icon';

@Component({
  selector: 'app-conversation-header',
  standalone: true,
  imports: [MatCardModule, MatIconModule],
  template: `
    <div class="conversation-header">
      <h3>{{ displayName }}</h3>

      @if (icebreakerQuestion) {
        <mat-card class="icebreaker-banner">
          <mat-card-content>
            <div class="icebreaker-content">
              <mat-icon class="icebreaker-icon">chat_bubble</mat-icon>
              <div>
                <span class="icebreaker-label">Icebreaker</span>
                <p class="icebreaker-text">{{ icebreakerQuestion }}</p>
                @if (icebreakerCategory) {
                  <span class="icebreaker-category">{{ icebreakerCategory }}</span>
                }
              </div>
            </div>
          </mat-card-content>
        </mat-card>
      }
    </div>
  `,
  styles: [`
    .conversation-header { margin-bottom: 16px; }
    .conversation-header h3 { margin: 0 0 8px 0; }
    .icebreaker-banner { background: #e3f2fd; margin: 8px 0; }
    .icebreaker-content { display: flex; align-items: flex-start; gap: 12px; }
    .icebreaker-icon { color: #1976d2; }
    .icebreaker-label { font-weight: 500; color: #1976d2; font-size: 0.85em; }
    .icebreaker-text { margin: 4px 0; font-style: italic; }
    .icebreaker-category { font-size: 0.75em; color: #666; }
  `],
})
export class ConversationHeaderComponent {
  @Input({ required: true }) displayName!: string;
  @Input() icebreakerQuestion: string | null = null;
  @Input() icebreakerCategory: string | null = null;
}
