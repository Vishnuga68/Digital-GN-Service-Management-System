const fs = require('fs');

const routesPath = './src/app/app.routes.ts';
let routesStr = fs.readFileSync(routesPath, 'utf8');

// Replace standard citizen mappings with nested routing
let newRoutesStr = routesStr.replace(
  /  { path: 'citizen', loadComponent: \(\) => import\('\.\/features\/citizen\/dashboard-page\/dashboard-page\.component'\)\.then\(m => m\.DashboardPageComponent\) },\r?\n/,
  \  {
    path: 'citizen',
    loadComponent: () => import('./layouts/citizen-layout/citizen-layout.component').then(m => m.CitizenLayoutComponent),
    children: [
      { path: '', loadComponent: () => import('./features/citizen/dashboard-page/dashboard-page.component').then(m => m.DashboardPageComponent) },
      { path: 'service-requests', loadComponent: () => import('./features/citizen/service-requests-page/service-requests-page.component').then(m => m.ServiceRequestsPageComponent) },
      { path: 'appointments', loadComponent: () => import('./features/citizen/appointments-page/appointments-page.component').then(m => m.AppointmentsPageComponent) },
      { path: 'registry', loadComponent: () => import('./features/citizen/residence-registry-page/residence-registry-page.component').then(m => m.ResidenceRegistryPageComponent) },
      { path: 'announcements', loadComponent: () => import('./features/citizen/announcements-page/announcements-page.component').then(m => m.AnnouncementsPageComponent) },
      { path: 'complaints', loadComponent: () => import('./features/citizen/grievances-page/grievances-page.component').then(m => m.GrievancesPageComponent) },
      { path: 'messages', loadComponent: () => import('./features/citizen/chat-page/chat-page.component').then(m => m.ChatPageComponent) },
      { path: 'downloads', loadComponent: () => import('./features/citizen/downloads-page/downloads-page.component').then(m => m.DownloadsPageComponent) },
      { path: 'help', loadComponent: () => import('./features/citizen/how-to-use-page/how-to-use-page.component').then(m => m.HowToUsePageComponent) },
      { path: 'notifications', loadComponent: () => import('./features/citizen/notifications-page/notifications-page.component').then(m => m.NotificationsPageComponent) },
    ]
  },
\
);

// Delete the old separate routes
const pathsToRemove = [
  'citizen/service-requests', 'citizen/appointments', 'citizen/registry',
  'citizen/announcements', 'citizen/complaints', 'citizen/messages',
  'citizen/downloads', 'citizen/help', 'citizen/notifications'
];

for (const p of pathsToRemove) {
  const reg = new RegExp(\  { path: '\', loadComponent: .*? },\\r?\\n\);
  newRoutesStr = newRoutesStr.replace(reg, '');
}

fs.writeFileSync(routesPath, newRoutesStr);

// Clean up notifications TS
const nPath = './src/app/features/citizen/notifications-page/';
let nTs = fs.readFileSync(nPath + 'notifications-page.component.ts', 'utf8');

// remove sidebar and profile things
nTs = nTs.replace(/  isSidebarExpanded = true;\r?\n  isProfileModalOpen = false;\r?\n  isNotificationModalOpen = false;\r?\n  citizenName = 'John Doe';\r?\n  userImageUrl = ''; \/\/ Default profile will be used\r?\n/, '');
nTs = nTs.replace(/  toggleSidebar\(\) \{[\s\S]*?log\('Logging out\.\.\.'\);\r?\n  \}\r?\n\r?\n/m, '');
fs.writeFileSync(nPath + 'notifications-page.component.ts', nTs);

