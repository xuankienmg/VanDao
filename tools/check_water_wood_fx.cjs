const fs=require('fs'),vm=require('vm'),assert=require('assert');
const context=vm.createContext({Image:class{set src(v){this.naturalWidth=this.naturalHeight=768;if(this.onload)this.onload()}},Map});
for(const p of ['wwwroot/js/fire-fx-data.js','wwwroot/js/water-wood-fx-data.js','wwwroot/js/fire-fx.js'])vm.runInContext(fs.readFileSync(p,'utf8'),context);
const specs=vm.runInContext('WATER_WOOD_FX_DATA',context);
for(const s of specs){
  assert(fs.existsSync('wwwroot/'+s.src),s.skillId+' missing sheet');
  for(const realm of [0,3,6]){
    const spec=vm.runInContext(`fireFxSpec(${JSON.stringify(s.skillId)},${realm})`,context);
    assert(spec&&spec.skillId===s.skillId);
    const got=new Set();
    for(let i=0;i<750;i++){
      const p=vm.runInContext(`fireFxPlan(${JSON.stringify(s.skillId)},${i/1000},${realm})`,context);
      assert(p&&p.frame>=0&&p.frame<16);got.add(p.frame);
    }
    assert.equal(got.size,16,s.skillId+' missing frame');
    for(const t of [-1,.75,1,NaN])assert.equal(context.fireFxPlan(s.skillId,t,realm),null);
    assert(context.fireFxAvailable(s.skillId,realm));
  }
}
assert.equal(context.fireFxSpec('hoa_cau',0).variant,'small');
assert.equal(context.fireFxSpec('hoa_cau',1).variant,'medium');
assert.equal(context.fireFxSpec('hoa_cau',6).variant,'large');
assert.equal(context.fireFxSpec('missing'),null);
assert(context.fireFxCacheInfo().size<=2);
console.log(`PASS ${specs.length} spell mappings, all 16 frames across 3 realms, rest/invalid input, fire variants and cache limit.`);
