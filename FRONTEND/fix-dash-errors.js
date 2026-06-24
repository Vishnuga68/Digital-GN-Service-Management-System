const fs = require('fs');
const dTsP = 'src/app/features/citizen/dashboard-page/dashboard-page.component.ts';
let dTsStr = fs.readFileSync(dTsP, 'utf8');
if (dTsStr.endsWith('}\n}\n')) {
    fs.writeFileSync(dTsP, dTsStr.slice(0, -2));
}

const dScssP = 'src/app/features/citizen/dashboard-page/dashboard-page.component.scss';
let dScssStr = fs.readFileSync(dScssP, 'utf8');
if (dScssStr.includes('\n}')) {
const lastBracketIndex = dScssStr.lastIndexOf('\n}');
    if (lastBracketIndex > dScssStr.length - 10) {
        fs.writeFileSync(dScssP, dScssStr.substring(0, lastBracketIndex) + dScssStr.substring(lastBracketIndex + 2));
    }
}
