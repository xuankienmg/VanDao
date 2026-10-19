const fs=require('node:fs'),vm=require('node:vm'),assert=require('node:assert/strict'),path=require('node:path');
const root=path.resolve(__dirname,'../..');
const images=[];
class FakeImage { constructor(){this.complete=false;this.naturalWidth=0;images.push(this)} set src(v){this._src=v} get src(){return this._src} load(){this.complete=true;this.naturalWidth=768;this.naturalHeight=768;this.onload?.()} fail(){this.onerror?.()} }
const entries=['hoa_cau_small','hoa_cau_medium','hoa_cau_large','liet_diem','hoa_long','phan_thien','xich_diem_thien','than_hoa','hu_khong_hoa'].map(name=>({skillId:name.startsWith('hoa_cau_')?'hoa_cau':name,variant:name.startsWith('hoa_cau_')?name.slice(8):'default',src:`img/fx/fire/${name}.png`,frameWidth:192,frameHeight:192,columns:4,frameCount:16,motion:name.startsWith('hoa_cau')||name==='hoa_long'?'projectile':'ground',anchorX:96,anchorY:148,scale:1.5,phases:[[0,.25,0,3],[.25,.45,4,7],[.45,.60,8,11],[.60,.75,12,15]]}));
const env={Image:FakeImage,FIRE_FX_DATA:entries};vm.createContext(env);
vm.runInContext(fs.readFileSync(path.join(root,'wwwroot/js/fire-fx.js'),'utf8'),env);
const noMetadata={Image:FakeImage};vm.createContext(noMetadata);vm.runInContext(fs.readFileSync(path.join(root,'wwwroot/js/fire-fx.js'),'utf8'),noMetadata);
assert.equal(noMetadata.fireFxAvailable('hoa_cau',0),false,'Missing metadata must preserve old renderer');
assert.equal(noMetadata.fireFxPlan('than_hoa',.4,4),null);
for(const s of [-.01,.75,1,NaN,Infinity])assert.equal(env.fireFxPlan('than_hoa',s,4),null);
const boundaries=[[0,0],[.2499,3],[.25,4],[.4499,7],[.45,8],[.5999,11],[.60,12],[.7499,15]];
for(const [s,f]of boundaries)assert.equal(env.fireFxPlan('than_hoa',s,4).frame,f);
assert.notEqual(env.fireFxPlan('than_hoa',.45,4).src,env.fireFxPlan('phan_thien',.45,1).src);
assert.notEqual(env.fireFxPlan('hu_khong_hoa',.45,5).src,env.fireFxPlan('xich_diem_thien',.45,2).src);
for(const [realm,variant]of [[0,'small'],[1,'medium'],[2,'large'],[5,'large']])assert.match(env.fireFxPlan('hoa_cau',.4,realm).src,new RegExp(variant));
assert.equal(env.fireFxPlan('thuy_nguyet',.3,0),null);
let draw=0,save=0,restore=0;const stack=[];
const ctx={canvas:{width:420,height:300},globalAlpha:.7,imageSmoothingEnabled:true,save(){save++;stack.push([this.globalAlpha,this.imageSmoothingEnabled])},restore(){restore++;[this.globalAlpha,this.imageSmoothingEnabled]=stack.pop()},drawImage(...a){draw++;assert.ok(a[5]>=0&&a[6]>=0);assert.ok(a[5]+a[7]<=420.001&&a[6]+a[8]<=300.001);assert.ok(Number.isFinite(this.globalAlpha))}};
const layout={handX:75,handY:180,targetX:340,targetY:180,groundY:250,heroHeight:100,realmIndex:4,reducedMotion:false};
const p=env.fireFxPlan('than_hoa',.45,4);
assert.equal(env.drawFireFx(ctx,p,layout),false,'Loading returns false so callers can fallback');
images.at(-1).load();assert.equal(env.drawFireFx(ctx,p,layout),true);assert.equal(draw,1);assert.equal(save,restore);assert.equal(ctx.globalAlpha,.7);assert.equal(ctx.imageSmoothingEnabled,true);
ctx.canvas.width=840;ctx.canvas.height=600;ctx.getTransform=()=>({a:2,d:2});assert.equal(env.drawFireFx(ctx,p,layout),true);ctx.canvas.width=420;ctx.canvas.height=300;delete ctx.getTransform;
assert.equal(env.drawFireSkillFx(ctx,'than_hoa',.75,4,layout),true,'Rest must suppress fallback');
assert.equal(env.drawFireSkillFx(ctx,'than_hoa',-.1,4,layout),false);
for(const s of [.1,.3,.45,.7]){const q=env.fireFxPlan('hoa_cau',s,0);env.drawFireFx(ctx,q,layout);images.at(-1).load();assert.equal(env.drawFireFx(ctx,q,layout),true)}
env.fireFxAvailable('liet_diem',0);images.at(-1).fail();assert.equal(env.fireFxAvailable('liet_diem',0),false);
for(const id of ['hoa_long','phan_thien','xich_diem_thien','than_hoa','hu_khong_hoa']){env.fireFxAvailable(id,5);images.at(-1).load();assert.ok(env.fireFxCacheInfo().size<=2)}
assert.equal(env.drawFireFx(ctx,null,layout),false);
console.log('PASS: phase boundaries, variants, distinct new skills, load/error fallback, canvas bounds, context balance and 2-sheet cache.');
