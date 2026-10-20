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
