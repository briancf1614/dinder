import { Injectable, signal } from '@angular/core';

/** Placeholder for SignalR connection management.
 *  Will be implemented in Phase 4 (Real-Time Chat). */
@Injectable({ providedIn: 'root' })
export class SignalRService {
  readonly isConnected = signal(false);

  // Future: HubConnection from @microsoft/signalr
  // connect(hubUrl: string, accessToken: string): void { ... }
  // disconnect(): void { ... }
}
