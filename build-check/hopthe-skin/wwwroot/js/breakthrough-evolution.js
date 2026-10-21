/* Chi tiết tu hành tích lũy: giữ sprite/trang phục thật, thêm hiệu ứng canvas theo cảnh giới. */
const BT_DETAILS = [
  {name:'Linh tức', desc:'Linh khí lưu chuyển theo nhịp thở'},
  {name:'Đài sen', desc:'Cánh sen linh lực nâng đỡ đạo cơ'},
  {name:'Kim đan', desc:'Kim đan phát sáng trước đan điền'},
  {name:'Nguyên anh', desc:'Linh ảnh nguyên anh hiện sau vai'},
  {name:'Kiếm khí', desc:'Ba đạo kiếm khí hộ thân'},
  {name:'Hư không', desc:'Vòng không gian bao quanh tu sĩ'}
];
function btEvolutionState(realmIndex, stage, stages, motion, phase, successAge, fromRealm){
  const rank = Math.max(0, Math.min(BT_DETAILS.length - 1, realmIndex));
  const progress = Math.max(0, Math.min(1, phase));
  return {
    rank, points:Math.max(1, Math.min(stages, stage + 1)), total:Math.max(1, stages),
    time:motion, breath:Math.sin(motion * 1.7),
    lift:progress ? Math.sin(progress * Math.PI) * 3.5 : Math.sin(motion * 1.2) * .65,
    gather:progress < .65 ? progress / .65 : Math.max(0, (1 - progress) / .35),
    reveal:successAge == null || fromRealm >= rank ? 1 : Math.min(1, Math.max(0, successAge - .25) / 1.2)
  };
}
function btEvolutionLayer(g, layer, state, pal, sprite, spriteBox){
  const {rank,time:t,breath,gather,reveal} = state;
  const visible = level => rank < level ? 0 : level === rank ? reveal : 1;
  const stroke = (color, alpha, width, draw) => {
    g.save(); g.globalAlpha = alpha; g.strokeStyle = color; g.lineWidth = width;
    g.shadowColor = color; g.shadowBlur = 1.8; g.beginPath(); draw(); g.stroke(); g.restore();
  };
  g.save();
  if (layer === 'back') {
    // Một vòng tiến cảnh, mỗi tầng nhỏ thêm một hạt sáng, độc lập với số cảnh giới.
    for (let i = 0; i < state.total; i++) {
      const a = Math.PI + (i + .5) / state.total * Math.PI;
      const x = 48 + Math.cos(a) * 31, y = 43 + Math.sin(a) * 31;
      g.globalAlpha = i < state.points ? .8 : .15; g.fillStyle = i < state.points ? pal.rune : '#8798b2';
      g.beginPath(); g.arc(x,y,i < state.points ? .65 : .4,0,Math.PI*2); g.fill();
    }
    // Luyện Khí: hai dải linh tức, nhịp chậm thay cho lắc cả ảnh nhân vật.
    for (const side of [-1,1]) stroke(pal.rune,.24 + gather * .3,.35,()=>{
      g.moveTo(48 + side * 5,68);
      g.bezierCurveTo(48 + side * (25 + breath),59,48 + side * 24,36,48 + side * 10,29);
    });
    // Trúc Cơ: các cánh sen phía sau chân; vẫn còn ở mọi cảnh giới cao hơn.
    if (rank >= 1) {
      for (let i = 0; i < 7; i++) {
        const a = -Math.PI + i / 6 * Math.PI, x = 48 + Math.cos(a) * 16, y = 72 + Math.sin(a) * 5;
        g.save(); g.globalAlpha = visible(1) * (.24 + .06 * breath + gather * .2);
        g.translate(x,y); g.rotate((i-3)*.24); g.fillStyle = pal.rune;
        g.beginPath(); g.moveTo(0,3); g.quadraticCurveTo(-5,-3,0,-10); g.quadraticCurveTo(5,-3,0,3); g.fill(); g.restore();
      }
    }
    // Nguyên Anh: dùng linh ảnh của chính sprite đang mặc, không thay nhân vật bằng hình khác.
    if (rank >= 3 && sprite?._sil && spriteBox) {
      const w = spriteBox.w * .26, h = spriteBox.h * .26;
      g.globalAlpha = visible(3) * (.55 + .08 * breath); g.shadowColor = '#ddbaff'; g.shadowBlur = 3;
      g.drawImage(sprite,61 - w/2,25 - h/2 + Math.sin(t)*1.1,w,h);
      stroke('#e6c6ff',visible(3)*.5,.35,()=>g.ellipse(61,26,5,7,0,0,Math.PI*2));
    }
    // Hóa Thần: ba đạo kiếm khí quay chậm, không cắt qua khuôn mặt.
    if (rank >= 4) for (let i = 0; i < 3; i++) {
      const a = t * .3 + i * Math.PI * 2 / 3;
      const x = 48 + Math.cos(a)*29, y = 44 + Math.sin(a)*20;
      g.save(); g.translate(x,y); g.rotate(a + Math.PI/2); g.globalAlpha = visible(4)*.65;
      g.strokeStyle = '#e8f4ff'; g.fillStyle = pal.rune; g.lineWidth = .35;
      g.beginPath(); g.moveTo(0,-5); g.lineTo(1,1); g.lineTo(0,2); g.lineTo(-1,1); g.closePath(); g.fill(); g.stroke();
      g.beginPath(); g.moveTo(-2,2); g.lineTo(2,2); g.moveTo(0,2); g.lineTo(0,4); g.stroke(); g.restore();
    }
    // Luyện Hư: vòng xoắn không gian đặt sau toàn bộ nhân vật.
    if (rank >= 5) for (const side of [-1,1]) stroke('#83b7ff',visible(5)*(.28 + gather*.2),.6,()=>{
      g.ellipse(48,43,34,13,side*.8 + Math.sin(t*.2)*.08,t*.18,t*.18+Math.PI*1.6);
    });
  } else {
    if (rank >= 2) {
      const x = 48, y = 56 - state.lift, r = 1.3 + breath*.12 + gather*.6;
      g.globalAlpha = visible(2)*.9;
      const glow = g.createRadialGradient(x,y,.2,x,y,5);
      glow.addColorStop(0,'#fff8c8'); glow.addColorStop(.3,'#f6ce62'); glow.addColorStop(1,'#e6a83b00');
      g.fillStyle=glow; g.beginPath(); g.arc(x,y,5,0,Math.PI*2); g.fill();
      g.fillStyle='#ffe997'; g.beginPath(); g.arc(x,y,r,0,Math.PI*2); g.fill();
      stroke('#fff3bb',visible(2)*.7,.3,()=>g.ellipse(x,y,3.1,1.2,t*.6,0,Math.PI*2));
    }
    if (rank >= 1) stroke(pal.rune,visible(1)*.35,.4,()=>g.ellipse(48,73,21,3.7,0,0,Math.PI));
  }
  g.restore();
}
