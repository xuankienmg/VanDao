const http=require('node:http'),fs=require('node:fs'),path=require('node:path');
const root=path.resolve(__dirname,'../../..');
const types={'.html':'text/html; charset=utf-8','.js':'text/javascript','.png':'image/png','.css':'text/css'};
http.createServer((req,res)=>{
 const url=new URL(req.url,'http://localhost');
 const name=url.pathname==='/'?'/tools/fire_fx/demo/index.html':decodeURIComponent(url.pathname);
 const file=path.resolve(root,'.'+name);
 if(!file.startsWith(root+path.sep)){res.writeHead(403);res.end();return;}
 res.setHeader('Content-Type',types[path.extname(file)]||'application/octet-stream');
 fs.readFile(file,(err,data)=>{res.statusCode=err?404:200;res.end(err?'Not found':data)});
}).listen(8791,'127.0.0.1',()=>console.log('Demo: http://127.0.0.1:8791/tools/fire_fx/demo/index.html'));
