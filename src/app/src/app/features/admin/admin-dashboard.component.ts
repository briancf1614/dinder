import { Component, inject, signal, OnInit, OnDestroy, ElementRef, ViewChild, afterNextRender } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { MatCardModule } from '@angular/material/card';
import { MatButtonToggleModule } from '@angular/material/button-toggle';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { AnalyticsService, AnalyticsResult } from './analytics.service';

@Component({
  selector: 'app-admin-dashboard',
  standalone: true,
  imports: [FormsModule, MatCardModule, MatButtonToggleModule, MatProgressSpinnerModule],
  template: `
    <div class="dashboard">
      <h2>Analytics Dashboard</h2>

      <mat-button-toggle-group [ngModel]="selectedDays()" (ngModelChange)="onDaysChange($event)" class="time-filter">
        <mat-button-toggle [value]="7">7 Days</mat-button-toggle>
        <mat-button-toggle [value]="30">30 Days</mat-button-toggle>
        <mat-button-toggle [value]="90">90 Days</mat-button-toggle>
      </mat-button-toggle-group>

      <div class="charts-grid">
        <mat-card>
          <mat-card-header><mat-card-title>Daily Active Users</mat-card-title></mat-card-header>
          <mat-card-content>
            @if (dauLoading()) {
              <mat-spinner diameter="40"></mat-spinner>
            } @else if (dauError()) {
              <p class="error">{{ dauError() }}</p>
            } @else {
              <canvas #dauCanvas width="600" height="200" class="chart-canvas"></canvas>
            }
          </mat-card-content>
        </mat-card>

        <mat-card>
          <mat-card-header><mat-card-title>Subscription Conversion Rate (%)</mat-card-title></mat-card-header>
          <mat-card-content>
            @if (conversionLoading()) {
              <mat-spinner diameter="40"></mat-spinner>
            } @else if (conversionError()) {
              <p class="error">{{ conversionError() }}</p>
            } @else {
              <canvas #conversionCanvas width="600" height="200" class="chart-canvas"></canvas>
            }
          </mat-card-content>
        </mat-card>

        <mat-card>
          <mat-card-header><mat-card-title>Match Rate (%)</mat-card-title></mat-card-header>
          <mat-card-content>
            @if (matchesLoading()) {
              <mat-spinner diameter="40"></mat-spinner>
            } @else if (matchesError()) {
              <p class="error">{{ matchesError() }}</p>
            } @else {
              <canvas #matchesCanvas width="600" height="200" class="chart-canvas"></canvas>
            }
          </mat-card-content>
        </mat-card>
      </div>
    </div>
  `,
  styles: [`
    .dashboard { padding: 16px; }
    h2 { margin: 0 0 16px 0; }
    .time-filter { margin-bottom: 16px; }
    .charts-grid { display: flex; flex-direction: column; gap: 16px; }
    .chart-canvas { width: 100%; max-height: 200px; }
    .error { color: #d32f2f; }
  `],
})
export class AdminDashboardComponent implements OnInit {
  private readonly analyticsService = inject(AnalyticsService);

  @ViewChild('dauCanvas') dauCanvasRef!: ElementRef<HTMLCanvasElement>;
  @ViewChild('conversionCanvas') conversionCanvasRef!: ElementRef<HTMLCanvasElement>;
  @ViewChild('matchesCanvas') matchesCanvasRef!: ElementRef<HTMLCanvasElement>;

  readonly selectedDays = signal(30);

  readonly dauLoading = signal(false);
  readonly dauError = signal<string | null>(null);
  readonly dauData = signal<AnalyticsResult | null>(null);

  readonly conversionLoading = signal(false);
  readonly conversionError = signal<string | null>(null);
  readonly conversionData = signal<AnalyticsResult | null>(null);

  readonly matchesLoading = signal(false);
  readonly matchesError = signal<string | null>(null);
  readonly matchesData = signal<AnalyticsResult | null>(null);

  private dauResult: AnalyticsResult | null = null;
  private conversionResult: AnalyticsResult | null = null;
  private matchesResult: AnalyticsResult | null = null;

  ngOnInit(): void {
    this.loadAll();
  }

  onDaysChange(days: number): void {
    this.selectedDays.set(days);
    this.loadAll();
  }

  private loadAll(): void {
    this.loadDAU();
    this.loadConversion();
    this.loadMatches();
  }

  private loadDAU(): void {
    this.dauLoading.set(true);
    this.analyticsService.getDAU(this.selectedDays()).subscribe({
      next: (data) => {
        this.dauData.set(data);
        this.dauLoading.set(false);
        this.drawChart(this.dauCanvasRef, data, '#1976d2');
      },
      error: (err) => {
        this.dauError.set('Failed to load DAU data');
        this.dauLoading.set(false);
      },
    });
  }

  private loadConversion(): void {
    this.conversionLoading.set(true);
    this.analyticsService.getConversion(this.selectedDays()).subscribe({
      next: (data) => {
        this.conversionData.set(data);
        this.conversionLoading.set(false);
        // Draw after next render cycle
        setTimeout(() => this.drawChart(this.conversionCanvasRef, data, '#388e3c'));
      },
      error: () => {
        this.conversionError.set('Failed to load conversion data');
        this.conversionLoading.set(false);
      },
    });
  }

  private loadMatches(): void {
    this.matchesLoading.set(true);
    this.analyticsService.getMatches(this.selectedDays()).subscribe({
      next: (data) => {
        this.matchesData.set(data);
        this.matchesLoading.set(false);
        setTimeout(() => this.drawChart(this.matchesCanvasRef, data, '#f57c00'));
      },
      error: () => {
        this.matchesError.set('Failed to load match data');
        this.matchesLoading.set(false);
      },
    });
  }

  private drawChart(canvasRef: ElementRef<HTMLCanvasElement> | undefined, data: AnalyticsResult, color: string): void {
    if (!canvasRef) return;
    const canvas = canvasRef.nativeElement;
    const ctx = canvas.getContext('2d');
    if (!ctx) return;

    const points = data.dataPoints;
    if (points.length === 0) return;

    const w = canvas.width;
    const h = canvas.height;
    const padding = 40;

    ctx.clearRect(0, 0, w, h);

    // Find max value
    const maxVal = Math.max(...points.map(p => p.value), 1);
    const xStep = (w - padding * 2) / Math.max(points.length - 1, 1);

    // Draw axes
    ctx.strokeStyle = '#ccc';
    ctx.lineWidth = 1;
    ctx.beginPath();
    ctx.moveTo(padding, padding);
    ctx.lineTo(padding, h - padding);
    ctx.lineTo(w - padding, h - padding);
    ctx.stroke();

    // Draw line chart
    ctx.strokeStyle = color;
    ctx.lineWidth = 2;
    ctx.beginPath();
    points.forEach((point, i) => {
      const x = padding + i * xStep;
      const y = padding + (h - padding * 2) * (1 - point.value / maxVal);
      if (i === 0) ctx.moveTo(x, y);
      else ctx.lineTo(x, y);
    });
    ctx.stroke();

    // Draw dots
    ctx.fillStyle = color;
    points.forEach((point, i) => {
      const x = padding + i * xStep;
      const y = padding + (h - padding * 2) * (1 - point.value / maxVal);
      ctx.beginPath();
      ctx.arc(x, y, 3, 0, Math.PI * 2);
      ctx.fill();
    });

    // Draw labels
    ctx.fillStyle = '#666';
    ctx.font = '10px sans-serif';
    ctx.textAlign = 'center';

    // Show every Nth label to avoid crowding
    const labelStep = Math.max(1, Math.floor(points.length / 7));
    points.forEach((point, i) => {
      if (i % labelStep === 0 || i === points.length - 1) {
        const x = padding + i * xStep;
        ctx.fillText(point.date.slice(5), x, h - padding + 15);
      }
    });
  }
}
