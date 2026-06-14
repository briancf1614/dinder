import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

export interface CandidateDto {
  profileId: string;
  userId: string;
  displayName: string;
  age: number;
  gender: string;
  bio: string | null;
  photoCount: number;
  prompts: { promptId: string; answer: string }[];
}

export interface CandidatesResult {
  candidates: CandidateDto[];
  nextCursor: string | null;
}

export interface SwipeRequest {
  targetProfileId: string;
  direction: 'Like' | 'Pass';
}

export interface SwipeResult {
  isMatch: boolean;
  matchId: string | null;
}

export interface MatchDto {
  matchId: string;
  profileId: string;
  displayName: string;
  conversationId: string;
  matchedAt: string;
}

export interface BoostResult {
  boostId: string;
  expiresAt: string;
}

@Injectable({ providedIn: 'root' })
export class DiscoveryService {
  private readonly apiBase = '/api/v1/discovery';
  private readonly http = inject(HttpClient);

  getCandidates(latitude?: number, longitude?: number, cursor?: string, limit: number = 20): Observable<CandidatesResult> {
    let params: Record<string, string | number> = { limit };
    if (latitude != null) params['latitude'] = latitude;
    if (longitude != null) params['longitude'] = longitude;
    if (cursor) params['cursor'] = cursor;
    return this.http.get<CandidatesResult>(`${this.apiBase}/candidates`, { params: params as any });
  }

  swipe(request: SwipeRequest): Observable<SwipeResult> {
    return this.http.post<SwipeResult>(`${this.apiBase}/swipe`, request);
  }

  getMatches(cursor?: string): Observable<{ matches: MatchDto[]; nextCursor: string | null }> {
    let params: Record<string, string> = {};
    if (cursor) params['cursor'] = cursor;
    return this.http.get<{ matches: MatchDto[]; nextCursor: string | null }>(`${this.apiBase}/matches`, { params });
  }

  undoLastSwipe(): Observable<void> {
    return this.http.post<void>(`${this.apiBase}/undo`, {});
  }

  boost(): Observable<BoostResult> {
    return this.http.post<BoostResult>(`${this.apiBase}/boost`, {});
  }
}
