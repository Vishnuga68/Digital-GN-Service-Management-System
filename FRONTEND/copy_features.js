const fs = require('fs');
const path = require('path');

const src = 'c:\\Users\\hp\\Documents\\ACEDEMIC\\SEMESTER 8\\GN-SYSTEM\\FRONTEND\\src\\app\\features\\citizen';
const dest = 'c:\\Users\\hp\\Documents\\ACEDEMIC\\SEMESTER 8\\GN-SYSTEM\\FRONTEND\\src\\app\\features\\gn-officer';

const features = [
  'service-requests-page',
  'appointments-page',
  'announcements-page',
  'chat-page',
  'downloads-page',
  'residence-registry-page'
];

features.forEach(feature => {
  const srcDir = path.join(src, feature);
  const destDir = path.join(dest, feature);
  if (fs.existsSync(srcDir)) {
    console.log('Copying ' + feature);
    fs.cpSync(srcDir, destDir, {recursive: true, force: true});
    
    // Replace "/citizen" with "/gn-officer" in TS and HTML
    const files = fs.readdirSync(destDir);
    files.forEach(file => {
      const filePath = path.join(destDir, file);
      if (file.endsWith('.ts') || file.endsWith('.html')) {
        let content = fs.readFileSync(filePath, 'utf-8');
        content = content.replace(/\/citizen/g, '/gn-officer');
        fs.writeFileSync(filePath, content);
      }
    });
  }
});
