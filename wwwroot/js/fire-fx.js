/* Hỏa hệ: một timeline cho preview và trận đánh, ảnh chỉ chứa hiệu ứng. */
const fireFxCache = new Map();
function fireFxSpec(skillId, realmIndex = 0){
  const variant = skillId === 'hoa_cau' ? (realmIndex <= 0 ? 'small' : realmIndex === 1 ? 'medium' : 'large') : 'default';
  const fire = typeof FIRE_FX_DATA === 'undefined' || !Array.isArray(FIRE_FX_DATA) ? [] : FIRE_FX_DATA;
  const extra = typeof WATER_WOOD_FX_DATA === 'undefined' || !Array.isArray(WATER_WOOD_FX_DATA) ? [] : WATER_WOOD_FX_DATA;
  const data = [...fire, ...extra];
  // Hợp Thể kế thừa timeline Luyện Hư, đổi kim diễm cho tuyệt kỹ mới.
  const sourceId = skillId === 'hon_nguyen_hoa' ? 'hu_khong_hoa' : skillId;
  const spec = data.find(s => s.skillId === sourceId && (s.variant || 'default') === variant);
  return spec ? {...spec, filter: skillId === 'hon_nguyen_hoa' ? 'hue-rotate(35deg) saturate(.75) brightness(1.2)' : 'none'} : null;
}
function fireFxPlan(skillId, s01, realmIndex = 0){
  const spec = fireFxSpec(skillId, realmIndex);
  if (!spec || !Number.isFinite(s01) || s01 < 0 || s01 >= .75) return null;
  const phase = spec.phases.findIndex(p => s01 >= p[0] && s01 < p[1]);
  if (phase < 0) return null;
  const [start, end, first, last] = spec.phases[phase];
  const frame = Math.min(last, first + Math.floor((s01 - start) / (end - start) * (last - first + 1)));
  return {...spec, frame, phase, s01, anchorKind: spec.motion === 'projectile' ? (phase === 0 ? 'hand' : phase === 1 ? 'projectile' : 'target') : spec.motion === 'target' ? 'target' : 'ground', alpha: phase === 3 ? Math.max(0, 1 - (s01 - .60) / .15) : 1};
}
function fireFxImage(src){
  let entry = fireFxCache.get(src);
  if (entry){ fireFxCache.delete(src); fireFxCache.set(src, entry); return entry; }
  if (fireFxCache.size >= 2){
    const oldest = fireFxCache.keys().next().value, old = fireFxCache.get(oldest);
    old.image.onload = old.image.onerror = null; old.image.src = ''; fireFxCache.delete(oldest);
  }
  const image = new Image(); entry = {image, state:'loading'}; fireFxCache.set(src, entry);
  image.onload = () => { entry.state = image.naturalWidth === 768 && image.naturalHeight === 768 ? 'ready' : 'error'; };
  image.onerror = () => { entry.state = 'error'; };
  image.src = src;
  return entry;
}
function fireFxAvailable(skillId, realmIndex = 0){
  const spec = fireFxSpec(skillId, realmIndex);
  return !!spec && fireFxImage(spec.src).state === 'ready';
}
function fireFxCacheInfo(){ return {size:fireFxCache.size, sources:[...fireFxCache.keys()]}; }
function drawFireFx(ctx, plan, layout){
  if (!plan) return false;
  const entry = fireFxImage(plan.src); if (entry.state !== 'ready') return false;
  const {handX,handY,targetX,targetY,groundY,heroHeight} = layout;
  let x = targetX, y = plan.anchorKind === 'ground' ? groundY : targetY;
  if (plan.anchorKind === 'hand'){ x = handX; y = handY; }
  if (plan.anchorKind === 'projectile'){
    const t = Math.max(0,Math.min(1,(plan.s01 - .25) / .20)), k = t*t*(3-2*t);
    x = handX + (targetX - handX)*k; y = handY + (targetY - handY)*k;
  }
  if (![x,y,heroHeight].every(Number.isFinite) || heroHeight <= 0) return false;
  // Layout dùng tọa độ logic; canvas có thể có backing store DPR / preview ×4.
  const transform = ctx.getTransform?.(), scaleX = Math.abs(transform?.a || 1), scaleY = Math.abs(transform?.d || 1);
  const w = ctx.canvas.width/scaleX, h = ctx.canvas.height/scaleY;
  if (w <= 2 || h <= 2) return false;
  const ax = plan.anchorX / plan.frameWidth, ay = plan.anchorY / plan.frameHeight;
  x = Math.max(1,Math.min(w-1,x)); y = Math.max(1,Math.min(h-1,y));
  let size;
  if (plan.visibleBounds && plan.targetScale){
    const [left,top,right,bottom]=plan.visibleBounds;
    const bw=(right-left)/plan.frameWidth, bh=(bottom-top)/plan.frameHeight;
    const targetSize=Math.max(layout.targetWidth || heroHeight,layout.targetHeight || heroHeight);
    const impactSize=targetSize*plan.targetScale/Math.max(bw,bh);
    const chargeSize=heroHeight*.55/Math.max(bw,bh);
    const travel=plan.anchorKind==='hand'?0:plan.anchorKind==='projectile'?Math.max(0,Math.min(1,(plan.s01-.25)/.2)):1;
    const wanted=plan.motion==='projectile'?chargeSize+(impactSize-chargeSize)*travel:impactSize;
    const l=Math.max(0,ax-left/plan.frameWidth),r=Math.max(0,right/plan.frameWidth-ax);
    const u=Math.max(0,ay-top/plan.frameHeight),d=Math.max(0,bottom/plan.frameHeight-ay);
    // Fit visible art, not transparent padding. Ground effects end at the feet.
    size=Math.min(wanted,(w-4)/bw,(h-4)/bh,
      l>0?(x-2)/l:Infinity,r>0?(w-2-x)/r:Infinity,
      u>0?(y-2)/u:Infinity,d>0?(h-2-y)/d:Infinity);
  } else {
    size = Math.min(heroHeight*plan.scale, w*.9, h*.9, x/ax, (w-x)/(1-ax), y/ay, (h-y)/(1-ay));
  }
  if (!Number.isFinite(size) || size <= 0) return false;
  const frame = layout.reducedMotion ? [2,6,9,14][plan.phase] : plan.frame;
  ctx.save();
  try {
    ctx.filter = plan.filter || 'none';
    ctx.globalAlpha *= plan.alpha * (layout.reducedMotion ? .8 : 1);
    ctx.imageSmoothingEnabled = plan.element === 'thuy' || plan.element === 'moc';
    ctx.drawImage(entry.image, frame%plan.columns*plan.frameWidth, Math.floor(frame/plan.columns)*plan.frameHeight, plan.frameWidth,plan.frameHeight,x-ax*size,y-ay*size,size,size);
  } finally { ctx.restore(); }
  return true;
}
/** true kể cả pha nghỉ: chặn hiệu ứng cũ bật lại ở cuối lượt. */
function drawFireSkillFx(ctx, skillId, s01, realmIndex, layout){
  if (!Number.isFinite(s01) || s01 < 0 || s01 > 1 || !fireFxAvailable(skillId, realmIndex)) return false;
  const plan = fireFxPlan(skillId, s01, realmIndex);
  return plan ? drawFireFx(ctx, plan, layout) : true;
}
