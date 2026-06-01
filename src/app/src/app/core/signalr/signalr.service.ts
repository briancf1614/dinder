import { Injectable, signal, inject } from '@angular/core';
import * as signalR from '@microsoft/signalr';
import { AuthService } from '../auth/auth.service';

/** Manages SignalR connections for real-time chat and notifications. */
@Injectable({ providedIn: 'root' })
export class SignalRService {
  private readonly authService = inject(AuthService);

  readonly isConnected = signal(false);

  private chatHub?: signalR.HubConnection;
  private notificationHub?: signalR.HubConnection;

  /** Build a HubConnection with JWT access token in query string. */
  private buildConnection(hubUrl: string): signalR.HubConnection {
    const token = this.authService.getAccessToken();
    return new signalR.HubConnectionBuilder()
      .withUrl(hubUrl, { accessTokenFactory: () => token ?? '' })
      .withAutomaticReconnect()
      .configureLogging(signalR.LogLevel.Information)
      .build();
  }

  /** Connect to the Chat hub. */
  async connectChat(): Promise<void> {
    if (this.chatHub?.state === signalR.HubConnectionState.Connected) return;

    this.chatHub = this.buildConnection('/hubs/chat');

    this.chatHub.onreconnecting(() => this.isConnected.set(false));
    this.chatHub.onreconnected(() => this.isConnected.set(true));
    this.chatHub.onclose(() => this.isConnected.set(false));

    await this.chatHub.start();
    this.isConnected.set(true);
  }

  /** Connect to the Notifications hub. */
  async connectNotifications(): Promise<void> {
    if (this.notificationHub?.state === signalR.HubConnectionState.Connected) return;

    this.notificationHub = this.buildConnection('/hubs/notifications');

    this.notificationHub.onreconnecting(() => this.isConnected.set(false));
    this.notificationHub.onreconnected(() => this.isConnected.set(true));
    this.notificationHub.onclose(() => this.isConnected.set(false));

    await this.notificationHub.start();
    this.isConnected.set(true);
  }

  /** Get the Chat hub connection. */
  getChatHub(): signalR.HubConnection | undefined {
    return this.chatHub;
  }

  /** Get the Notification hub connection. */
  getNotificationHub(): signalR.HubConnection | undefined {
    return this.notificationHub;
  }

  /** Disconnect all hubs. */
  async disconnect(): Promise<void> {
    if (this.chatHub) {
      await this.chatHub.stop();
      this.chatHub = undefined;
    }
    if (this.notificationHub) {
      await this.notificationHub.stop();
      this.notificationHub = undefined;
    }
    this.isConnected.set(false);
  }
}
