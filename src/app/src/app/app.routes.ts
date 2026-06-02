import { Routes } from '@angular/router';

export const routes: Routes = [
  {
    path: 'login',
    loadComponent: () => import('./features/auth/login.page').then(m => m.LoginPageComponent),
  },
  {
    path: 'register',
    loadComponent: () => import('./features/auth/register.page').then(m => m.RegisterPageComponent),
  },
  {
    path: 'discovery',
    loadComponent: () => import('./features/discovery/discovery.page').then(m => m.DiscoveryPageComponent),
  },
  {
    path: 'profile/edit',
    loadComponent: () => import('./features/profile/profile.page').then(m => m.ProfilePageComponent),
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
  { path: '**', redirectTo: '/discovery' },
];
