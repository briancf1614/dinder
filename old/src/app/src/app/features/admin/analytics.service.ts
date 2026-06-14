import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

export interface AnalyticsDataPoint {
  date: string;
  value: number;
}

export interface AnalyticsResult {
  metric: string;
  days: number;
  dataPoints: AnalyticsDataPoint[];
}

@Injectable({ providedIn: 'root' })
export class AnalyticsService {
  private readonly apiBase = '/api/v1/admin/analytics';
  private readonly http = inject(HttpClient);

  getDAU(days: number = 30): Observable<AnalyticsResult> {
    return this.http.get<AnalyticsResult>(`${this.apiBase}/dau`, { params: { days: String(days) } });
  }

  getConversion(days: number = 30): Observable<AnalyticsResult> {
    return this.http.get<AnalyticsResult>(`${this.apiBase}/conversion`, { params: { days: String(days) } });
  }

  getMatches(days: number = 30): Observable<AnalyticsResult> {
    return this.http.get<AnalyticsResult>(`${this.apiBase}/matches`, { params: { days: String(days) } });
  }
}
