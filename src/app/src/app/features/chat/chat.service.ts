import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';

export interface ConversationDto {
  conversationId: string;
  displayName: string;
  lastMessage: string | null;
  unreadCount: number;
  icebreakerQuestion: string | null;
  icebreakerCategory: string | null;
}

export interface ConversationsResult {
  conversations: ConversationDto[];
  nextCursor: string | null;
}

@Injectable({ providedIn: 'root' })
export class ChatService {
  private readonly apiBase = '/api/v1/chat';
  private readonly http = inject(HttpClient);

  getConversations(cursor?: string, limit: number = 20): Observable<ConversationsResult> {
    let params = new HttpParams().set('limit', limit.toString());
    if (cursor) {
      params = params.set('cursor', cursor);
    }
    return this.http.get<ConversationsResult>(`${this.apiBase}/conversations`, { params });
  }
}
