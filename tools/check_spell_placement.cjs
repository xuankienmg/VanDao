const fs=require('fs'),vm=require('vm'),assert=require('assert');
const scope=vm.createContext({Image:class{set src(v){this.naturalWidth=this.naturalHeight=768;this.onload?.()}},Map});
for(const p of ['fire-fx-data','water-wood-fx-data','fire-fx'])vm.runInContext(fs.readFileSync(`wwwroot/js/${p}.js`,'utf8'),scope);
const specs=vm.runInContext('WATER_WOOD_FX_DATA',scope);
let checks=0;
for(const s of specs)for(const view of [{w:960,h:440,g:370,x:690,hero:120},{w:120,h:60,g:52,x:94,hero:46}])for(const mult of [1,1.25,1.5]){
  let draw;
  const ctx={canvas:{width:view.w,height:view.h},globalAlpha:1,getTransform:()=>({a:1,d:1}),save(){},restore(){},drawImage(...a){draw=a}};
  scope.fireFxAvailable(s.skillId,6);
  const target=view.hero*mult;
  const layout={handX:view.w*.27,handY:view.g-view.hero*.55,targetX:view.x,targetY:view.g-target*.5,groundY:view.g,heroHeight:view.hero,targetWidth:target,targetHeight:target};
  let lastSize;
  for(const t of [.46,.50,.56]){
    const plan=scope.fireFxPlan(s.skillId,t,6);
    assert(scope.drawFireFx(ctx,plan,layout));
    const [,,,,,x,y,w,h]=draw;const [l,u,r,b]=s.visibleBounds;
    assert(w>0&&h>0);
    assert(x+l/192*w>=1.9&&x+r/192*w<=view.w-1.9,s.skillId+' horizontal clipping');
    assert(y+u/192*h>=1.9&&y+b/192*h<=view.h-1.9,s.skillId+' vertical clipping');
    if(s.motion==='ground')assert(Math.abs(y+s.anchorY/192*h-view.g)<1e-6,s.skillId+' ground alignment');
    else assert(Math.abs(y+s.anchorY/192*h-layout.targetY)<1e-6,s.skillId+' target alignment');
    if(lastSize)assert(Math.abs(lastSize-w)<1e-6,s.skillId+' size jumps between impact frames');
    lastSize=w;checks++;
  }
}
console.log(`PASS ${checks} placements: 16 spells, 3 enemy sizes, 2 viewports, 3 impact frames; visible bounds, anchors and stable scale.`);
