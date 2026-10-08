import fs from 'node:fs';
import path from 'node:path';
import {fileURLToPath} from 'node:url';
const root=path.resolve(path.dirname(fileURLToPath(import.meta.url)),'..');
fs.cpSync(path.join(root,'themes/media'),path.join(root,'demo/media'),{recursive:true});
for(const name of ['skin-engine.js','skin.css'])fs.copyFileSync(path.join(root,'shared',name),path.join(root,'demo',name));
console.log('Demo assets prepared. Serve the demo directory with a local static server.');