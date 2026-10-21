"""Slice the character-only edited sheet into consistently anchored action atlases."""
import sys, json
from pathlib import Path
from PIL import Image
import numpy as np

root=Path(__file__).resolve().parents[1]
out=root/'wwwroot/img/skin'
im=Image.open(sys.argv[1]).convert('RGBA')
if im.size != (1024,1024):
    if im.width != im.height: raise ValueError(f'Expected square sheet, got {im.size}')
    im=im.resize((1024,1024),Image.Resampling.LANCZOS)
source_art=root/'tools/art-sources/skin'
source_art.mkdir(parents=True,exist_ok=True)
im.save(source_art/'luyenkhi_actions_clean.png')
# Each pose uses the same source pixel scale. Anchors are the midpoint of stance,
# not the center of the weapon or trailing hair. Row baselines remain fixed.
groups={
 'walk': (0,260,252,[(i*128,(i+1)*128,i*128+84) for i in range(8)]),
 'run': (265,490,477,[(i*146,min(1024,(i+1)*146),i*146+87) for i in range(7)]),
 'slash': (500,765,750,[(0,200,120),(200,435,290),(435,630,515),(630,835,735),(835,1024,920)]),
 'cast': (780,1024,1014,[(0,220,121),(220,480,335),(480,710,573)])
}
manifest={'source':'existing reference edited with built-in image_gen; effects removed','frameWidth':384,'frameHeight':320,'anchor':[192,308],'bodyHeight':210,'actions':{}}
for name,(top,bottom,baseline,boxes) in groups.items():
    # Label whole-row silhouettes before assigning poses, so weapons and trailing
    # robes are never cut at arbitrary equal-width cell boundaries.
    arr=np.array(im.crop((0,top,1024,bottom)))
    mask=arr[:,:,3]>32; seen=np.zeros(mask.shape,bool); components=[]
    h,w=mask.shape
    for yy,xx in zip(*np.nonzero(mask)):
        if seen[yy,xx]: continue
        todo=[(xx,yy)]; pts=[]; seen[yy,xx]=True
        while todo:
            x,y=todo.pop(); pts.append((x,y))
            for dx,dy in ((-1,-1),(0,-1),(1,-1),(-1,0),(1,0),(-1,1),(0,1),(1,1)):
                nx,ny=x+dx,y+dy
                if 0<=nx<w and 0<=ny<h and mask[ny,nx] and not seen[ny,nx]:
                    seen[ny,nx]=True; todo.append((nx,ny))
        if len(pts)>250: components.append(pts)
    if len(components)!=len(boxes): raise ValueError(f'{name}: expected {len(boxes)} silhouettes, found {len(components)}')
    components.sort(key=lambda pts:sum(x for x,y in pts)/len(pts))
    frames=[]
    for (left,right,anchor),pts in zip(boxes,components):
        xs,ys=zip(*pts); left,right=min(xs),max(xs)+1
        owned=np.zeros_like(arr); owned[ys,xs]=arr[ys,xs]
        crop=Image.fromarray(owned).crop((left,0,right,h))
        frame=Image.new('RGBA',(384,320))
        frame.alpha_composite(crop,(192-(anchor-left),308-(baseline-top)))
        frames.append(frame)
    atlas=Image.new('RGBA',(384*len(frames),320))
    for i,f in enumerate(frames): atlas.alpha_composite(f,(384*i,0))
    atlas.save(out/f'luyenkhi_{name}_v2.png')
    frames[0].save(out/f'luyenkhi_{name}_v2.gif',save_all=True,append_images=frames[1:],duration=100 if name=='run' else 160,loop=0,disposal=2)
    manifest['actions'][name]={'src':f'img/skin/luyenkhi_{name}_v2.png','frames':len(frames),'loop':name in ('walk','run')}
(out/'luyenkhi_actions.json').write_text(json.dumps(manifest,ensure_ascii=False,indent=2),encoding='utf-8')
print(json.dumps(manifest,ensure_ascii=False))
