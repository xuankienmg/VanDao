// Read-only inventory. Candidates require review: runtime paths can be constructed dynamically.
const fs=require('node:fs'),path=require('node:path'),cp=require('node:child_process'),crypto=require('node:crypto');
const root=path.resolve(__dirname,'..'),out=path.join(root,'obj/source-audit');fs.mkdirSync(out,{recursive:true});
const walk=(dir)=>fs.readdirSync(dir,{withFileTypes:true}).flatMap(e=>e.name==='.git'?[]:e.isDirectory()?walk(path.join(dir,e.name)):[path.join(dir,e.name)]);
const rel=f=>path.relative(root,f).replaceAll('\\','/'),files=walk(root),size=f=>fs.statSync(f).size,mb=n=>+(n/1048576).toFixed(2);
const tracked=new Set(cp.execFileSync('git',['ls-files','-z'],{cwd:root,encoding:'utf8'}).split('\0').filter(Boolean));
const group=(list,key)=>{const g={};for(const f of list){const k=key(f);g[k]??={files:0,bytes:0};g[k].files++;g[k].bytes+=size(f);}return Object.entries(g).map(([folder,v])=>({folder,files:v.files,MB:mb(v.bytes)})).sort((a,b)=>b.MB-a.MB)};
const runtime=files.filter(f=>/^(Game|Desktop|Data|Properties|wwwroot)\//.test(rel(f))||/^(Program\.cs|VanDao\.csproj)$/.test(rel(f))).filter(f=>/\.(cs|js|css|html|json|csproj)$/i.test(f));
const source=runtime.map(f=>({file:rel(f),text:fs.readFileSync(f,'utf8')})),text=source.map(x=>x.text).join('\n');
const assets=files.filter(f=>rel(f).startsWith('wwwroot/')&&!/\.(js|css|html)$/i.test(f));
const candidates=assets.filter(f=>{
 const name=path.basename(f),stem=path.basename(f,path.extname(f)),base=stem.replace(/_(?:24|48|hd)$/,'');
 const p=rel(f);
 // Known runtime families: actor sheets, evolved pets and programmatically selected nav icons.
 if(/^wwwroot\/img\/skin\/(trucco|kimdan|luyenhu|hopthe)_(actions|flight)\.png$/.test(p))return false;
 if(/^wwwroot\/img\/skin\/luyenkhi_(walk|run|slash|cast)_v2\.png$/.test(p))return false;
 if(/^wwwroot\/img\/header\/nav-(req|inv|craft|map|skill|pet)\.png$/.test(p))return false;
 const pet=p.match(/^wwwroot\/img\/pet\/(.+?)(?:_loi)?_[012](?:_hd)?\.png$/);
 if(pet&&text.includes('"'+pet[1]+'"')&&!(pet[1]==='thanh_long'&&!p.includes('_loi_')))return false;
 // Conservative: keep any asset whose stem/base occurs anywhere in runtime source.
 return !text.includes(name)&&!text.includes(stem)&&!text.includes(base);
}).map(f=>({path:rel(f),bytes:size(f),dynamicDirectory:/^wwwroot\/img\/(pet|skin|header|fx)\//.test(rel(f))}));
const codeCandidates=files.filter(f=>/^wwwroot\/(js|css)\//.test(rel(f))).filter(f=>!source.some(s=>s.file!==rel(f)&&s.text.includes(path.basename(f)))).map(f=>({path:rel(f),bytes:size(f)}));
const hashGroups=new Map();for(const f of assets){const h=crypto.createHash('sha256').update(fs.readFileSync(f)).digest('hex');if(!hashGroups.has(h))hashGroups.set(h,[]);hashGroups.get(h).push(f);}
const duplicates=[...hashGroups.values()].filter(g=>g.length>1).map(g=>({paths:g.map(rel),bytesEach:size(g[0]),redundantBytes:size(g[0])*(g.length-1)})).sort((a,b)=>b.redundantBytes-a.redundantBytes);
const items=JSON.parse(fs.readFileSync(path.join(root,'obj/source-audit-msbuild.json'),'utf8')).Items;
const buildItems=Object.fromEntries(Object.entries(items).map(([k,list])=>[k,group(list.map(x=>x.FullPath).filter(f=>fs.existsSync(f)),f=>rel(f).split('/')[0])]));
const report={generated:new Date().toISOString(),disk:group(files,f=>rel(f).split('/')[0]),tracked:group(files.filter(f=>tracked.has(rel(f))),f=>rel(f).split('/')[0]),buildItems,unreferencedCode:codeCandidates,assetCandidates:candidates,identicalAssets:duplicates};
fs.writeFileSync(path.join(out,'audit.json'),JSON.stringify(report,null,2));
for(const k of ['disk','tracked','buildItems','unreferencedCode'])console.log(k,JSON.stringify(report[k]));
console.log('assetCandidates',candidates.length,'MB',mb(candidates.reduce((s,f)=>s+f.bytes,0)),'duplicates',duplicates.length,'redundantMB',mb(duplicates.reduce((s,g)=>s+g.redundantBytes,0)));
console.log('largestCandidates',JSON.stringify([...candidates].sort((a,b)=>b.bytes-a.bytes).slice(0,20)));
