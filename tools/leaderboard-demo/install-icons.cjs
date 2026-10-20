const fs=require('node:fs');
const html=fs.readFileSync('tools/leaderboard-demo/index.html','utf8');
const svg=html.match(/<svg width="0"[\s\S]*?<\/svg>/)[0].replaceAll('id="','id="lb-');
fs.appendFileSync('wwwroot/js/leaderboard.js','\nconst lbSymbols = '+JSON.stringify(svg)+';\nfunction lbIcon(name){return `<svg class="lb-icon" viewBox="0 0 64 64" aria-hidden="true"><use href="#lb-${name}"/></svg>`}\n');
