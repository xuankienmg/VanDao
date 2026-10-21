/* Companion only: existing combat HUD and layout stay intact, no separate pet HP panel. */
const petBattleImages=new Map();
function petBattleImage(src){
  if(!src)return null;
  if(!petBattleImages.has(src)){const image=new Image();image.src=src;petBattleImages.set(src,image);}
  const image=petBattleImages.get(src);return image.complete&&image.naturalWidth?image:null;
}
function drawPetCompanion(context,battle,phase,options){
  const pet=battle?.pet;if(!pet?.image)return;
  const image=petBattleImage(pet.image);if(!image)return;
  const {heroX,ground,heroSize,sx,sy,time,reduced,width}=options;
  const cast=phase.name==='fight'?(pet.casts||[]).find(c=>c.turn===phase.i):null;
  const active=cast&&phase.s>=.2&&phase.s<.72;
  const h=heroSize*sy*.50,w=h*image.naturalWidth/image.naturalHeight;
  const x=Math.max(w/2+3,(heroX-heroSize*.66)*sx),feet=ground*sy;
  const jump=active&&!reduced?Math.sin((phase.s-.2)/.52*Math.PI)*h*.12:0;
  context.save();context.imageSmoothingEnabled=true;context.globalAlpha=phase.name==='lose'?.7:1;
  context.fillStyle='rgba(2,12,13,.35)';context.beginPath();context.ellipse(x,feet,w*.32,Math.max(1,h*.05),0,0,Math.PI*2);context.fill();
  context.drawImage(image,x-w/2,feet-h-jump,w,h);
  if(active&&!reduced){
    const lightningDragon=pet.element==='loi'&&pet.name==='Thanh Long';
    const color=lightningDragon?'#fff1ac':({hoa:'#ff9d54',bang:'#a8e4fc',moc:'#99d4a3',tho:'#d4bd83',loi:'#cfb0ff',thuy:'#82cddd',gio:'#cce9bb'}[pet.element]||'#e1cc93');
    const progress=(phase.s-.2)/.52;
    context.globalAlpha=Math.sin(progress*Math.PI)*.75;context.strokeStyle=color;context.lineWidth=2;
    context.beginPath();context.ellipse(x,feet-h*.35,w*(.25+progress*.25),h*(.2+progress*.1),0,0,Math.PI*2);context.stroke();
    for(let i=0;i<5;i++){const a=i*Math.PI*2/5+progress*2;context.fillStyle=color;context.beginPath();context.arc(x+Math.cos(a)*w*.4,feet-h*.45+Math.sin(a)*h*.3,1.5,0,Math.PI*2);context.fill();}
    if(lightningDragon&&cast.dmg>0&&cast.kind==='dragon'&&Number.isFinite(options.targetX)){
      const tx=options.targetX*sx,ty=feet-h*.5;
      for(let strike=0;strike<3;strike++){
        const pulse=Math.max(0,1-Math.abs(progress-(.25+strike*.25))*6);if(!pulse)continue;
        context.globalAlpha=pulse;context.strokeStyle=strike===2?'#ffffff':color;context.lineWidth=strike===2?2.5:1.5;
        context.beginPath();context.moveTo(x,feet-h*.55);
        for(let step=1;step<=8;step++){const k=step/8;context.lineTo(x+(tx-x)*k,ty+(step===8?0:Math.sin(step*3.7+strike*2)*h*.18));}
        context.stroke();
      }
    }
    context.globalAlpha=1;
  }
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
