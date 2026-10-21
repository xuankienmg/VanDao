/* Static concept artwork: full-resolution painting behind the existing combat layers. */
const conceptBackdrop=new Image();
conceptBackdrop.src='img/backgrounds/concept-courtyard-v1.webp';
function conceptBackdropReady(){return conceptBackdrop.complete&&conceptBackdrop.naturalWidth>0;}
function drawConceptBackdrop(context,width,height){
  if(!conceptBackdropReady())return false;
  const iw=conceptBackdrop.naturalWidth,ih=conceptBackdrop.naturalHeight,k=Math.max(width/iw,height/ih);
  context.save();context.imageSmoothingEnabled=true;context.imageSmoothingQuality='high';
  context.drawImage(conceptBackdrop,(width-iw*k)/2,(height-ih*k)/2,iw*k,ih*k);
  // Quiet values behind HUD without changing the layout or hiding environmental detail.
  context.fillStyle='rgba(3,14,21,.13)';context.fillRect(0,0,width,height);context.restore();return true;
}
