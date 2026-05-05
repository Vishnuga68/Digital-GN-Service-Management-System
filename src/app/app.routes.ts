import { Routes } from '@angular/router';

export const routes: Routes = [
  { path: '', redirectTo: 'auth', pathMatch: 'full' },
  { path: 'auth', loadComponent: () => import('./features/auth/auth.component').then(m => m.AuthComponent) },
  { 
    path: 'citizen', 
    loadComponent: () => import('./layouts/citizen-layout/citizen-layout.component').then(m => m.CitizenLayoutComponent),
    children: [
      { path: '', loadComponent: () => import('./features/citizen/dashboard-page/dashboard-page.component').then(m => m.DashboardPageComponent) },
      { path: 'service-requests', loadComponent: () => import('./features/citizen/service-requests-page/service-requests-page.component').then(m => m.ServiceRequestsPageComponent) },
      { path: 'service-requests/new', loadComponent: () => import('./features/citizen/service-request-new-page/service-request-new-page.component').then(m => m.ServiceRequestNewPageComponent) },
      { path: 'service-requests/:id', loadComponent: () => import('./features/citizen/request-details-page/request-details-page.component').then(m => m.RequestDetailsPageComponent) },
      { path: 'appointments', loadComponent: () => import('./features/citizen/appointments-page/appointments-page.component').then(m => m.AppointmentsPageComponent) },
      { path: 'registry', loadComponent: () => import('./features/citizen/residence-registry-page/residence-registry-page.component').then(m => m.ResidenceRegistryPageComponent) },
      { path: 'announcements', loadComponent: () => import('./features/citizen/announcements-page/announcements-page.component').then(m => m.AnnouncementsPageComponent) },

      { path: 'messages', loadComponent: () => import('./features/citizen/chat-page/chat-page.component').then(m => m.ChatPageComponent) },
      { path: 'downloads', loadComponent: () => import('./features/citizen/downloads-page/downloads-page.component').then(m => m.DownloadsPageComponent) },
      { path: 'help', loadComponent: () => import('./features/citizen/how-to-use-page/how-to-use-page.component').then(m => m.HowToUsePageComponent) },
      { path: 'notifications', loadComponent: () => import('./features/citizen/notifications-page/notifications-page.component').then(m => m.NotificationsPageComponent) }
    ]
  },
  { 
    path: 'gn-officer', 
    loadComponent: () => import('./layouts/gn-layout/gn-layout.component').then(m => m.GnLayoutComponent),
    children: [
      { path: '', loadComponent: () => import('./features/gn-officer/dashboard-page/dashboard-page.component').then(m => m.DashboardPageComponent) },
      { path: 'notifications', loadComponent: () => import('./features/citizen/notifications-page/notifications-page.component').then(m => m.NotificationsPageComponent) },
      { path: 'requests', loadComponent: () => import('./features/gn-officer/service-requests-page/service-requests-page.component').then(m => m.ServiceRequestsPageComponent) },
      { path: 'requests/:id', loadComponent: () => import('./features/gn-officer/request-details-page/request-details-page.component').then(m => m.RequestDetailsPageComponent) },
      { path: 'appointments', loadComponent: () => import('./features/gn-officer/appointments-page/appointments-page.component').then(m => m.AppointmentsPageComponent) },
      { path: 'announcements', loadComponent: () => import('./features/gn-officer/announcements-page/announcements-page.component').then(m => m.AnnouncementsPageComponent) },
      { path: 'messages', loadComponent: () => import('./features/gn-officer/chat-page/chat-page.component').then(m => m.ChatPageComponent) },
      { path: 'documents', loadComponent: () => import('./features/gn-officer/downloads-page/downloads-page.component').then(m => m.DownloadsPageComponent) },
      { path: 'registry', loadComponent: () => import('./features/gn-officer/residence-registry-page/residence-registry-page.component').then(m => m.ResidenceRegistryPageComponent) },
      { path: 'transfer', loadComponent: () => import('./features/gn-officer/transfer-requests-page/transfer-requests-page.component').then(m => m.TransferRequestsPageComponent) }
    ]
  },
  { 
    path: 'ds-officer', 
    loadComponent: () => import('./layouts/ds-layout/ds-layout.component').then(m => m.DsLayoutComponent),
    children: [
      { path: '', loadComponent: () => import('./features/ds-officer/dashboard-page/dashboard-page.component').then(m => m.DashboardPageComponent) },
      { path: 'monitoring', loadComponent: () => import('./features/ds-officer/gn-monitoring-page/gn-monitoring-page.component').then(m => m.GnMonitoringPageComponent) },
      { path: 'registration', loadComponent: () => import('./features/ds-officer/gn-registration-page/gn-registration-page.component').then(m => m.GnRegistrationPageComponent) },
      { path: 'transfers', loadComponent: () => import('./features/ds-officer/gn-transfers-page/gn-transfers-page.component').then(m => m.GnTransfersPageComponent) }
    ]
  }
];
