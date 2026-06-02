import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

export interface PromptCatalogItem {
  id: string;
  text: string;
  category: string;
  isEnabled: boolean;
}

export interface PromptItem {
  promptId: string;
  answer: string;
}

export interface UpdatePromptsRequest {
  prompts: PromptItem[];
}

export interface Profile {
  id: string;
  userId: string;
  displayName: string;
  bio: string | null;
  gender: string;
  isDiscoverable: boolean;
  prompts: PromptItem[];
  photos: { id: string; blobKey: string; status: string }[];
}

@Injectable({ providedIn: 'root' })
export class ProfileService {
  private readonly apiBase = '/api/v1/profile';
  private readonly http = inject(HttpClient);

  getProfile(): Observable<Profile> {
    return this.http.get<Profile>(this.apiBase);
  }

  updateProfile(request: {
    displayName: string;
    gender: string;
    bio?: string;
    prompts?: PromptItem[];
  }): Observable<Profile> {
    return this.http.put<Profile>(this.apiBase, request);
  }

  getPromptCatalog(): Observable<PromptCatalogItem[]> {
    return this.http.get<PromptCatalogItem[]>(`${this.apiBase}/prompts/catalog`);
  }

  updatePrompts(request: UpdatePromptsRequest): Observable<void> {
    return this.http.put<void>(`${this.apiBase}/prompts`, request);
  }
}
