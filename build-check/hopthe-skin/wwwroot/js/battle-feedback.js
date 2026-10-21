/* Chỉ hiện chữ né khi server xác nhận; không gieo lại xác suất ở client. */
function battleDodgeFeedback(battle,phase,seen,emit,x,y){
  if(phase.name!=='fight'||phase.s<.85||battle.dodged?.[phase.i]!==true||battle.monSkip?.[phase.i])return false;
  const key=`${phase.trip}:dodge:${phase.i}`;
  if(seen[key])return false;
  seen[key]=1;emit({type:'dodge',x,y,text:'Né tránh',life:0,max:.9});return true;
}
function drawBattleDodgeLabel(ctx,label,options){
  const {sx,sy,width,reduced,big}=options,k=Math.max(0,Math.min(1,label.life/label.max));
  ctx.save();
  try{
    ctx.globalAlpha=reduced?1:Math.min(1,k/.08)*Math.min(1,(1-k)/.25);
    ctx.font=`700 ${big?16:12}px "Be Vietnam Pro",system-ui,sans-serif`;
    ctx.textAlign='center';ctx.textBaseline='alphabetic';ctx.lineJoin='round';
    const half=ctx.measureText(label.text).width/2+3,x=Math.max(half,Math.min(width-half,label.x*sx)),y=label.y*sy-(reduced?0:k*14);
    ctx.lineWidth=3;ctx.strokeStyle='#132326';ctx.fillStyle=label.col||'#b3e9e3';
    ctx.strokeText(label.text,x,y);ctx.fillText(label.text,x,y);
  }finally{ctx.restore()}
}
function battleDivineFeedback(battle,phase,seen,emit,x,y){
  if(phase.name!=='fight')return;
  const turn=battle.divine?.turns?.[phase.i];if(!turn)return;
  const labels=[['boosted',.45,'Thiên cơ +100%','#ead09a'],['nullified',.85,'Vô hiệu hóa','#a6dacc'],['saved',.85,'Bất tử','#f0bd82']];
  for(const [flag,at,text,col] of labels){
    const key=`${phase.trip}:divine:${flag}:${phase.i}`;
    if(!turn[flag]||phase.s<at||seen[key])continue;
    seen[key]=1;emit({type:'dodge',x,y,text,col,life:0,max:1});
  }
}
function drawBattleDivine(ctx,battle,phase,width,height,big){
  const ability=battle.divine;if(!ability?.tier)return;
  const turn=ability.turns?.[phase.i||0];
  const charged=phase.name==='fight'&&(phase.s>=.85 ? turn?.charged :
    (phase.i>0 && ability.turns?.[phase.i-1]?.charged && !(phase.s>=.45&&turn?.boosted)));
  ctx.save();
  try{
    const y=height-9,col=charged?'#f2dc94':'#afc5b5';
    ctx.strokeStyle=col;ctx.fillStyle='#152420';ctx.lineWidth=1;
    ctx.beginPath();ctx.moveTo(13,y-10);ctx.lineTo(19,y-4);ctx.lineTo(13,y+2);ctx.lineTo(7,y-4);ctx.closePath();ctx.fill();ctx.stroke();
    ctx.font=`600 ${big?11:9}px "Be Vietnam Pro",system-ui,sans-serif`;ctx.textAlign='left';ctx.textBaseline='alphabetic';ctx.fillStyle=col;
    const quality={6:'Thần',7:'Tiên',8:'Đạo',9:'Huyền Tiên'}[ability.tier];
    const text=(big?ability.name:quality)+(charged?' · Sẵn sàng':'');
    ctx.fillText(text,24,y,Math.max(20,width-32));
  }finally{ctx.restore()}
}
/** Dấu trạng thái nét cao: hình dạng riêng, tâm nhân vật luôn thoáng. */
function drawBattleStatus(ctx,{kind,x,y,width,height,strength=1,time=0,reduced=false}){
  if(width<=0||height<=0)return;
  ctx.save();
  try{
    const rx=width*.54,ry=height*.54,pulse=reduced?1:.94+.06*Math.sin(time*2);
    ctx.lineWidth=Math.max(1.2,Math.min(2,width/45));ctx.lineJoin='round';ctx.lineCap='round';
    const diamond=(cx,cy,r)=>{ctx.beginPath();ctx.moveTo(cx,cy-r);ctx.lineTo(cx+r,cy);ctx.lineTo(cx,cy+r);ctx.lineTo(cx-r,cy);ctx.closePath();ctx.fill();ctx.stroke()};
    if(kind==='target'){
      ctx.strokeStyle='#dd8276';ctx.globalAlpha=.7;
      for(const [a,b] of [[.12,1.15],[2,3.03],[3.25,4.28],[5.15,6.18]]){ctx.beginPath();ctx.ellipse(x,y,rx,height*.09,0,a,b);ctx.stroke()}
      return;
    }
    if(kind==='shield'||kind==='ward'){
      ctx.globalAlpha=(.35+.45*Math.max(0,Math.min(1,strength)))*pulse;
      ctx.strokeStyle='#8ad9eb';ctx.fillStyle='rgba(88,177,199,.055)';
      ctx.beginPath();ctx.moveTo(x,y-ry);ctx.lineTo(x+rx*.85,y-ry*.65);ctx.lineTo(x+rx*.8,y+ry*.25);ctx.quadraticCurveTo(x+rx*.4,y+ry*.8,x,y+ry);ctx.quadraticCurveTo(x-rx*.4,y+ry*.8,x-rx*.8,y+ry*.25);ctx.lineTo(x-rx*.85,y-ry*.65);ctx.closePath();ctx.fill();ctx.stroke();
      // Hai cạnh trong gợi vỏ khiên, không phủ lên mặt nhân vật.
      for(const sign of [-1,1]){ctx.beginPath();ctx.moveTo(x+sign*rx*.69,y-ry*.5);ctx.lineTo(x+sign*rx*.65,y+ry*.15);ctx.stroke()}
      return;
    }
    if(kind==='control'){
      ctx.strokeStyle='#c4b8ed';ctx.fillStyle='#172135';
      // Hai gông khóa ở hai bên, kèm khóa nhỏ gần chân.
      for(const sign of [-1,1]){ctx.beginPath();ctx.moveTo(x+sign*rx*.65,y-ry*.5);ctx.lineTo(x+sign*rx,y-ry*.5);ctx.lineTo(x+sign*rx,y+ry*.55);ctx.lineTo(x+sign*rx*.65,y+ry*.55);ctx.stroke()}
      ctx.beginPath();ctx.arc(x,y+ry*.55,3,Math.PI,0);ctx.stroke();ctx.fillRect(x-4,y+ry*.55,8,6);ctx.strokeRect(x-4,y+ry*.55,8,6);return;
    }
    if(kind==='elite'){
      ctx.strokeStyle='#bc9be8';ctx.fillStyle='#352849';ctx.globalAlpha=.8*pulse;
      for(const sign of [-1,1]){diamond(x+sign*rx,y-ry*.2,3);ctx.beginPath();ctx.moveTo(x+sign*rx*.83,y+ry*.15);ctx.lineTo(x+sign*rx*.94,y+ry*.55);ctx.lineTo(x+sign*rx*.65,y+ry*.75);ctx.stroke()}
      return;
    }
    if(kind==='enrage'){
      ctx.strokeStyle='#eb7a56';ctx.globalAlpha=.85*pulse;
      for(const sign of [-1,1]){ctx.beginPath();ctx.moveTo(x+sign*rx*.75,y+ry*.8);ctx.lineTo(x+sign*rx,y+ry*.15);ctx.lineTo(x+sign*rx*.8,y+ry*.35);ctx.lineTo(x+sign*rx*.88,y-ry*.35);ctx.stroke()}
      return;
    }
    if(kind==='dash'){
      ctx.strokeStyle='#a4d5b8';ctx.globalAlpha=.7;
      for(let i=0;i<3;i++){ctx.beginPath();ctx.moveTo(x-rx*(1+i*.16),y+ry*(.2+i*.2));ctx.lineTo(x-rx*.45,y+ry*(.1+i*.2));ctx.stroke()}
    }
  }finally{ctx.restore()}
}
