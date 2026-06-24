const fs = require('fs');

const dPath = './src/app/features/citizen/dashboard-page/';
const lPath = './src/app/layouts/citizen-layout/';
const nPath = './src/app/features/citizen/notifications-page/';
const rPath = './src/app/';

// === 1. Fix Layout TS ===
let layoutTs = fs.readFileSync(lPath + 'citizen-layout.component.ts', 'utf8');
// Remove chart things from Layout TS
layoutTs = layoutTs.replace(/import { BaseChartDirective } from 'ng2-charts';\r?\n/, '');
layoutTs = layoutTs.replace(/import { ChartConfiguration, ChartData, ChartType } from 'chart\.js';\r?\n/, '');
layoutTs = layoutTs.replace(/BaseChartDirective, /, '');

// Remove chart properties from Layout TS
layoutTs = layoutTs.replace(/\/\/ Doughnut Chart Configuration[\s\S]*?public barChartType: ChartType = 'bar';\r?\n\r?\n/m, '');
// Remove dashboard specific recent activities etc
layoutTs = layoutTs.replace(/pendingRequests = 2;\r?\n  upcomingMeetings = 1;\r?\n  newMessages = 3;\r?\n\r?\n/m, '');
layoutTs = layoutTs.replace(/  recentActivities = \[[\s\S]*?\];\r?\n\r?\n/m, '');
fs.writeFileSync(lPath + 'citizen-layout.component.ts', layoutTs);

// === 2. Fix Dashboard TS ===
let dashTs = fs.readFileSync(dPath + 'dashboard-page.component.ts', 'utf8');
// Remove TranslationService and mostly modal methods from dashboard TS since they are in layout now
// We keep charts
dashTs = dashTs.replace(/  isSidebarExpanded = true;[\s\S]*?markAsRead\(id: number\) \{\r?\n.*?const notif.*?id;\r?\n.*?read = true;\r?\n.*\}\r?\n  \}\r?\n/m, '');
fs.writeFileSync(dPath + 'dashboard-page.component.ts', dashTs + '}\n'); // Quick close fix

// === 3. Extract SCSS properly ===
const scss = fs.readFileSync(dPath + 'dashboard-page.component.scss', 'utf8');
const scssParts = scss.split('  // Dashboard Body');
const layoutScssBody = scssParts[0] + '  // Dashboard Body\n  .dashboard-body {\n    flex: 1;\n    padding: 2rem;\n    overflow-y: auto;\n  }\n}\n\n' + scssParts[1].split('// ==========================================')[1];

const dashScssBody = '// Dashboard Body inside layout\n' + scssParts[1].split('// ==========================================')[0].trim() + '\n';
fs.writeFileSync(lPath + 'citizen-layout.component.scss', layoutScssBody);
fs.writeFileSync(dPath + 'dashboard-page.component.scss', dashScssBody);

// === 4. Notifications Page TS and HTML and SCSS Fix ===
let nHtml = fs.readFileSync(nPath + 'notifications-page.component.html', 'utf8');
// Remove the whole wrapper
nHtml = nHtml.split('<div class="page-body">')[1];
if (nHtml) {
  nHtml = '<div class="page-body">\n' + nHtml;
  nHtml = nHtml.replace(/  <\/main>\r?\n<\/div>\r?\n/m, '');
  fs.writeFileSync(nPath + 'notifications-page.component.html', nHtml);
}

// SCSS
let nScss = fs.readFileSync(nPath + 'notifications-page.component.scss', 'utf8');
let nScssClean = nScss.replace(/\.dashboard-layout \{[\s\S]*?\}\r?\n\r?\n/m, ''); 
// The regex above will fail if dashboard-layout wraps everything but wait..
// Notifications page might have dashboard-layout wrapped. Let's look closely...
