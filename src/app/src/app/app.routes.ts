import { Routes } from '@angular/router';

export const routes: Routes = [
  {
    path: 'profile/edit',
    loadComponent: () => import('./features/profile/prompt-picker.component').then(m => m.PromptPickerComponent),
  },
  {
    path: 'discovery',
    loadComponent: () => import('./features/discovery/discovery-card.component').then(m => m.DiscoveryCardComponent),
  },
  {
    path: 'chat/:conversationId',
    loadComponent: () => import('./features/chat/conversation-header.component').then(m => m.ConversationHeaderComponent),
  },
  {
    path: 'admin/dashboard',
    loadComponent: () => import('./features/admin/admin-dashboard.component').then(m => m.AdminDashboardComponent),
  },
  {
    path: 'report',
    loadComponent: () => import('./features/moderation/report-form.component').then(m => m.ReportFormComponent),
  },
  { path: '', redirectTo: '/discovery', pathMatch: 'full' },
];
