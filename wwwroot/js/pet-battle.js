/* Companion only: existing combat HUD and layout stay intact, no separate pet HP panel. */
const petBattleImages=new Map();
function petBattleImage(src){
  if(!src)return null;
  if(!petBattleImages.has(src)){const image=new Image();image.src=src;petBattleImages.set(src,image);}
  const image=petBattleImages.get(src);return image.complete&&image.naturalWidth?image:null;
}
// Existing art: fire atlases (4×4, 192px) and water moon/prison strips (160px).
// Pure turn progress drives every frame, including replay and sampled battle turns.
const PET_ATTACK_ART={
  fire:{src:'img/fx/fire/hoa_cau_small.png',size:192,columns:4,count:16},
  flame:{src:'img/fx/fire/liet_diem.png',size:192,columns:4,count:16},
  moon:{src:'img/fx_thuy_nguyet.png',size:160,columns:22,count:22},
  ward:{src:'img/fx_thuy_lao.png',size:160,columns:25,count:25}
};
let petAttackArtWarmed=false;
function warmPetAttackArt(){
  if(petAttackArtWarmed)return;
  petAttackArtWarmed=true;
  Object.values(PET_ATTACK_ART).forEach(art=>petBattleImage(art.src));
}
function petAttackKind(pet,cast){
  if(cast.kind)return cast.kind;
  if(cast.guard>0)return 'frost';if(cast.heal>0)return 'deer';
  return {hoa:'fox',loi:pet.name==='Thanh Long'?'dragon':'eagle',thuy:'leech',gio:'tiger'}[pet.element]||'cat';
}
function petAttackPlan(pet,cast,phase){
  if(phase.name!=='fight'||cast.turn!==phase.i||!Number.isFinite(phase.s)||phase.s<.18||phase.s>=.82)return null;
  const progress=(phase.s-.18)/.64,kind=petAttackKind(pet,cast);
  const strikes=['dragon','eagle'].includes(kind)?Math.max(1,Math.min(3,cast.hits?.length||3)):1;
  return {kind,progress,strikes,alpha:Math.min(1,progress/.08,(1-progress)/.16)};
}
function petAttackSprite(g,art,frame,x,y,size,filter='none',rotation=0,alpha=1){
  const image=petBattleImage(art.src);if(!image)return false;
  if(![x,y,size,rotation,alpha].every(Number.isFinite)||size<=0)return false;
  const f=Math.max(0,Math.min(art.count-1,Math.floor(frame)));
  g.save();
  try{
    g.globalAlpha*=alpha;g.filter=filter;g.imageSmoothingEnabled=true;
    g.translate(x,y);g.rotate(rotation);
    g.drawImage(image,(f%art.columns)*art.size,Math.floor(f/art.columns)*art.size,art.size,art.size,-size/2,-size/2,size,size);
  }finally{g.restore();}
  return true;
}
function drawPetAttackEffects(g,pet,phase,layout){
  const casts=(pet.casts||[]).filter(c=>c.turn===phase.i);
  if(phase.name!=='fight'||!casts.length)return;
  const {sourceX,sourceY,heroX,heroY,targetX,targetY,height,reduced}=layout;
  if(![sourceX,sourceY,heroX,heroY,targetX,targetY,height].every(Number.isFinite)||height<=0)return;
  g.save();
  try{
    // Clip glows and large impacts to the actual battle canvas.
    g.beginPath();g.rect(0,0,g.canvas.width,g.canvas.height);g.clip();
    for(const cast of casts){
      const plan=petAttackPlan(pet,cast,phase);if(!plan)continue;
      const {kind,progress:p,strikes}=plan;
      g.save();
      try{
        g.globalAlpha*=plan.alpha;
        const impact=Math.max(0,Math.min(1,(p-.44)/.56));
        if(reduced){
          // One soft, stationary frame; no flashing, travel or camera shake.
          const support=['frost','turtle','xuanwu','deer','crane','regen'].includes(kind);
          petAttackSprite(g,support?PET_ATTACK_ART.ward:kind==='fox'||kind==='phoenix'||kind==='burn'?PET_ATTACK_ART.flame:PET_ATTACK_ART.moon,support?8:15,support?heroX:targetX,support?heroY:targetY,height*.8,'saturate(.7)',0,.45);
          continue;
        }
        if(kind==='dragon'||kind==='eagle'){
          const gold=kind==='dragon';
          for(let i=0;i<strikes;i++){
            const at=.36+i*.12,pulse=Math.max(0,1-Math.abs(p-at)/.13);if(!pulse)continue;
            const tx=targetX+(i-1)*height*.10,ty=targetY+(i%2)*height*.07;
            // Thin electrical paths extend the existing companion lightning renderer.
            g.globalAlpha=plan.alpha*pulse;g.shadowColor=gold?'#ffe49a':'#c4acff';g.shadowBlur=height*.06;
            for(const [width,color] of [[height*.055,gold?'#eab74b':'#9070ed'],[height*.018,'#fff9e9']]){
              g.lineWidth=Math.max(1,width);g.strokeStyle=color;g.beginPath();g.moveTo(sourceX,sourceY);
              for(let step=1;step<=10;step++){const k=step/10;g.lineTo(sourceX+(tx-sourceX)*k,sourceY+(ty-sourceY)*k+(step===10?0:Math.sin(step*4.1+i*2)*height*.11));}
              g.stroke();
            }
            petAttackSprite(g,PET_ATTACK_ART.moon,15+i,tx,ty,height*.65,gold?'sepia(.8) saturate(1.5)':'hue-rotate(65deg)',0,pulse);
          }
        }else if(kind==='fox'||kind==='leech'){
          const flight=Math.max(0,Math.min(1,(p-.10)/.34)),ease=flight*flight*(3-2*flight);
          const x=sourceX+(targetX-sourceX)*ease,y=sourceY+(targetY-sourceY)*ease-Math.sin(ease*Math.PI)*height*.18;
          if(p<.46){
            petAttackSprite(g,kind==='fox'?PET_ATTACK_ART.fire:PET_ATTACK_ART.moon,kind==='fox'?Math.min(7,Math.floor(p*20)):10+Math.floor(p*12)%4,x,y,height*.8);
          }else petAttackSprite(g,kind==='fox'?PET_ATTACK_ART.fire:PET_ATTACK_ART.moon,kind==='fox'?8+Math.floor(impact*8):14+Math.floor(impact*8),targetX,targetY,height*(.85+impact*.25));
          if(kind==='leech'&&cast.heal>0&&p>.56){
            const k=(p-.56)/.44;
            petAttackSprite(g,PET_ATTACK_ART.moon,10+Math.floor(k*3),targetX+(heroX-targetX)*k,targetY+(heroY-targetY)*k,height*.45,'hue-rotate(-100deg)',0,.65);
          }
        }else if(kind==='cat'||kind==='tiger'){
          if(p>.34){
            const k=Math.min(1,(p-.34)/.56),gold=kind==='tiger';
            for(let i=0;i<3;i++)petAttackSprite(g,PET_ATTACK_ART.moon,10+Math.min(3,Math.floor(k*4)),targetX+(i-1)*height*.12,targetY+(i-1)*height*.08,height*(.75+k*.25),gold?'sepia(1) saturate(2.4)':'hue-rotate(65deg) saturate(1.5)',-.65+i*.16,1-k*.7);
          }
        }else if(kind==='phoenix'||kind==='burn'){
          if(kind==='burn'||p>.30){
            const k=kind==='burn'?p:Math.max(0,(p-.30)/.70);
            petAttackSprite(g,PET_ATTACK_ART.flame,Math.min(15,Math.floor(k*16)),targetX,targetY+height*.20,height*(kind==='burn'?.85:1.45));
          }
        }else if(['frost','turtle','xuanwu'].includes(kind)){
          const filter=kind==='turtle'?'hue-rotate(190deg) saturate(.8)':kind==='xuanwu'?'hue-rotate(-30deg)':'none';
          const f=p<.25?Math.floor(p/.25*7):p<.78?7+Math.floor(p*8)%4:18+Math.floor((p-.78)/.22*7);
          petAttackSprite(g,PET_ATTACK_ART.ward,f,heroX,heroY,height*1.1,filter,0,.8);
        }else if(kind==='deer'||kind==='crane'||kind==='regen'){
          const filter=kind==='crane'?'hue-rotate(-75deg) saturate(.4) brightness(1.3)':'hue-rotate(-110deg) saturate(.8)';
          petAttackSprite(g,PET_ATTACK_ART.ward,7+Math.floor(p*4)%4,heroX,heroY,height*(.8+p*.35),filter,0,.7);
          for(let i=0;i<3;i++)petAttackSprite(g,PET_ATTACK_ART.moon,10+i,heroX+(i-1)*height*.22,heroY+height*(.4-((p+i*.2)%1)*.8),height*.25,filter,0,.55);
        }else if(kind==='reflect'){
          const k=Math.max(0,Math.min(1,(p-.25)/.30));
          petAttackSprite(g,PET_ATTACK_ART.moon,k<1?10+Math.floor(k*3):14+Math.floor(impact*7),heroX+(targetX-heroX)*k,heroY+(targetY-heroY)*k,height*.75,'hue-rotate(-30deg)');
        }
      }finally{g.restore();}
    }
  }finally{g.restore();}
}
function drawPetCompanion(context,battle,phase,options){
  const pet=battle?.pet;if(!pet?.image)return;
  warmPetAttackArt();
  const image=petBattleImage(pet.image);
  const {heroX,ground,heroSize,sx,sy,time,reduced,width}=options;
  const cast=phase.name==='fight'?(pet.casts||[]).find(c=>c.turn===phase.i):null;
  const active=cast&&phase.s>=.2&&phase.s<.72;
  const h=heroSize*sy*.50,w=h*(image?image.naturalWidth/image.naturalHeight:1);
  const x=Math.max(w/2+3,(heroX-heroSize*.66)*sx),feet=ground*sy;
  const jump=active&&!reduced?Math.sin((phase.s-.2)/.52*Math.PI)*h*.12:0;
  context.save();context.imageSmoothingEnabled=true;context.globalAlpha=phase.name==='lose'?.7:1;
  context.fillStyle='rgba(2,12,13,.35)';context.beginPath();context.ellipse(x,feet,w*.32,Math.max(1,h*.05),0,0,Math.PI*2);context.fill();
  if(image)context.drawImage(image,x-w/2,feet-h-jump,w,h);
  if(active&&!reduced){
    const lightningDragon=pet.element==='loi'&&pet.name==='Thanh Long';
    const color=lightningDragon?'#fff1ac':({hoa:'#ff9d54',bang:'#a8e4fc',moc:'#99d4a3',tho:'#d4bd83',loi:'#cfb0ff',thuy:'#82cddd',gio:'#cce9bb'}[pet.element]||'#e1cc93');
    const progress=(phase.s-.2)/.52;
    context.globalAlpha=Math.sin(progress*Math.PI)*.75;context.strokeStyle=color;context.lineWidth=2;
    context.beginPath();context.ellipse(x,feet-h*.35,w*(.25+progress*.25),h*(.2+progress*.1),0,0,Math.PI*2);context.stroke();
    for(let i=0;i<5;i++){const a=i*Math.PI*2/5+progress*2;context.fillStyle=color;context.beginPath();context.arc(x+Math.cos(a)*w*.4,feet-h*.45+Math.sin(a)*h*.3,1.5,0,Math.PI*2);context.fill();}
    context.globalAlpha=1;
  }
  const targetX=Number.isFinite(options.targetX)?options.targetX*sx:heroX*sx;
  drawPetAttackEffects(context,pet,phase,{sourceX:x,sourceY:feet-h*.55,heroX:heroX*sx,heroY:feet-heroSize*sy*.48,targetX,targetY:feet-(options.targetSize||heroSize)*sy*.5,height:heroSize*sy,reduced});
  context.font=`600 ${options.big?11:9}px "Be Vietnam Pro",system-ui,sans-serif`;context.textAlign='center';context.lineWidth=3;context.strokeStyle='#0b191d';context.fillStyle='#f0d59b';
  const label=pet.name||'Linh thú',half=context.measureText(label).width/2+3,tx=Math.min(width-half,Math.max(half,x)),ty=feet-h-jump-5;
  context.strokeText(label,tx,ty);context.fillText(label,tx,ty);context.restore();
}
function petBattleFeedback(battle,phase,seen,emit,x,y){
  if(phase.name!=='fight'||phase.s<.45)return;
  for(const cast of battle.pet?.casts||[]){
    if(cast.turn!==phase.i)continue;
    const key=`${phase.trip}:pet:${phase.i}:${(battle.pet.casts||[]).indexOf(cast)}`;if(seen[key])continue;seen[key]=1;
    const amount=(cast.dmg>0?` −${cast.dmg}`:'')+(cast.heal>0?` · Hồi +${cast.heal}`:'')+(cast.guard>0?` · Khiên +${cast.guard}`:'');
    emit({type:'dodge',x,y,text:(cast.skill||'Linh thuật')+amount,col:'#d6dbaa',life:0,max:1});
  }
}
