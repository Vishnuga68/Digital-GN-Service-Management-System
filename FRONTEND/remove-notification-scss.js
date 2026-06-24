const fs = require('fs');
const path = './src/app/features/citizen/notifications-page/notifications-page.component.scss';
let scss = fs.readFileSync(path, 'utf8');

// The file might start with styles for sidebar, header, etc.
// Since we have moved everything to citizen-layout, let's delete everything
// and only keep styles for .page-body onwards.
// A simpler way: just let it cascade but remove .dashboard-layout override if it exists.
let match = scss.indexOf('.page-body {');
if (match !== -1) {
    fs.writeFileSync(path, scss.substring(match));
} else {
// try notifications-page-card
let match2 = scss.indexOf('.notifications-page-card {');
if (match2 !== -1) {
fs.writeFileSync(path, scss.substring(match2));
}
}
