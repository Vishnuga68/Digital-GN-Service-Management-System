const fs = require('fs');

const dPath = './src/app/features/citizen/dashboard-page/';
const lPath = './src/app/layouts/citizen-layout/';
const nPath = './src/app/features/citizen/notifications-page/';

// HTML
const html = fs.readFileSync(dPath + 'dashboard-page.component.html', 'utf8');
const layoutHtmlObj = html.split('<!-- DASHBOARD BODY -->');
const layoutHtmlHeader = layoutHtmlObj[0];
const layoutHtmlFooterBody = layoutHtmlObj[1].split('<!-- FOOTER -->');
const contentHTML = '<router-outlet></router-outlet>';
const newLayoutHtml = layoutHtmlHeader + '<!-- ROUTER OUTLET -->\n<div class="dashboard-body">\n' + contentHTML + '\n<!-- FOOTER -->\n' + layoutHtmlFooterBody[1];
const newDashboardHtml = layoutHtmlFooterBody[0].trim();

fs.writeFileSync(lPath + 'citizen-layout.component.html', newLayoutHtml);
fs.writeFileSync(dPath + 'dashboard-page.component.html', newDashboardHtml);

// SCSS
const scss = fs.readFileSync(dPath + 'dashboard-page.component.scss', 'utf8');
const scssParts = scss.split('  // Dashboard Body');
const newLayoutScss = scssParts[0] + '  // Dashboard Body\n  .dashboard-body {\n    flex: 1;\n    padding: 2rem;\n    overflow-y: auto;\n    > * { /* Children are injected via router-outlet */ }\n  }\n}\n\n' + scssParts[1].split('// ==========================================')[1]; // Modals logic

const dashboardScssPart = scssParts[1].split('// ==========================================')[0]; // Extract dashboard body part correctly later

// TS file copying (we will literally duplicate the class and edit it out manually)
const ts = fs.readFileSync(dPath + 'dashboard-page.component.ts', 'utf8');
fs.writeFileSync(lPath + 'citizen-layout.component.ts', ts.replace('DashboardPageComponent', 'CitizenLayoutComponent').replace('app-dashboard-page', 'app-citizen-layout').replace('./dashboard-page.component.html', './citizen-layout.component.html').replace('./dashboard-page.component.scss', './citizen-layout.component.scss'));

