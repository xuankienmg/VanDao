const fs=require('fs'),vm=require('vm'),assert=require('node:assert/strict');
const elements={mkNotice:{textContent:'',hidden:true,classList:{toggle(){}}}};
let fail=false,posts=0;
const c=vm.createContext({console,URLSearchParams,setInterval(){},setTimeout,performance,S:{id:'test'},ITEM:{herb:{name:'Hỏa Tâm Hoa'}},busy:false,$:id=>elements[id],fmt:String,api:async(method)=>{if(method==='POST'){posts++;if(fail)throw Error('Không đủ vật phẩm');return{state:{id:'test'}}}return{enabled:true,listings:[],mine:[]}},setState(){},notifyHost(){}});
vm.runInContext(fs.readFileSync('wwwroot/js/market.js','utf8'),c);
(async()=>{
 vm.runInContext("mkPick='herb';mkData={listingDays:3}",c);
 await vm.runInContext("mkList('herb',2,1000)",c);
 assert.equal(vm.runInContext('mkPick',c),null);
 assert.match(vm.runInContext('mkNotice',c),/Đã treo bán 2 Hỏa Tâm Hoa/);
 fail=true;vm.runInContext("mkPick='herb'",c);
 await vm.runInContext("mkList('herb',2,1000)",c);
 assert.equal(vm.runInContext('mkPick',c),'herb');
 assert.equal(elements.mkNotice.textContent,'Không đủ vật phẩm');
 c.busy=true;await vm.runInContext("mkList('herb',2,1000)",c);assert.equal(posts,2);
 console.log('PASS listing success resets selection and notifies; error retains selection; busy blocks duplicate submission');
})().catch(e=>{console.error(e);process.exitCode=1});
