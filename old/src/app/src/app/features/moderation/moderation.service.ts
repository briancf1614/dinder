import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

export interface ReportUserRequest {
  reportedUserId: string;
  reason: string;
  subCategory?: string;
  description?: string;
}

export interface ReportResult {
  reportId: string;
  isDuplicate: boolean;
}

export interface AppealPhotoRequest {
  reason: string;
}

export interface AppealPhotoResult {
  mediaFileId: string;
  status: string;
}

@Injectable({ providedIn: 'root' })
export class ModerationService {
  private readonly apiBase = '/api/v1/moderation';
  private readonly http = inject(HttpClient);

  reportUser(request: ReportUserRequest): Observable<ReportResult> {
    return this.http.post<ReportResult>(`${this.apiBase}/report`, request);
  }

  appealPhoto(mediaFileId: string, request: AppealPhotoRequest): Observable<AppealPhotoResult> {
    return this.http.post<AppealPhotoResult>(`/api/v1/media/photos/${mediaFileId}/appeal`, request);
  }
}
