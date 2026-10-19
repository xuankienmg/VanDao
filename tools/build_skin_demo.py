"""Build review-only sprite sheets and GIFs; does not modify game assets or code."""
import sys,json
from pathlib import Path
from PIL import Image
import numpy as np

skin,source=sys.argv[1:3]
out=Path(__file__).resolve().parents[1]/'docs/skin-demos'/skin
out.mkdir(parents=True,exist_ok=True)
im=Image.open(source).convert('RGBA')
im.save(out/'source.png')
arr=np.array(im); mask=arr[:,:,3]>40; seen=np.zeros(mask.shape,bool); parts=[]
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
    if len(pts)>1000: parts.append(pts)
if len(parts)!=16: raise ValueError(f'Expected 16 character silhouettes, found {len(parts)}')
parts.sort(key=lambda p:sum(y for x,y in p)/len(p))
all_frames=[]
for row in range(4):
    poses=sorted(parts[row*4:row*4+4],key=lambda p:sum(x for x,y in p)/len(p))
    for pts in poses:
        xs,ys=zip(*pts); l,r,t,b=min(xs),max(xs)+1,min(ys),max(ys)+1
        owned=np.zeros((b-t,r-l,4),dtype=np.uint8)
        for x,y in pts: owned[y-t,x-l]=arr[y,x]
        feet=[x for x,y in pts if y>b-35]; anchor=(min(feet)+max(feet))/2
        frame=Image.new('RGBA',(480,440))
        frame.alpha_composite(Image.fromarray(owned),(round(240-(anchor-l)),420-(b-t)))
        all_frames.append(frame)
packed=Image.new('RGBA',(480*4,440*4))
for i,f in enumerate(all_frames): packed.alpha_composite(f,((i%4)*480,(i//4)*440))
packed.save(out/'sheet.png')
actions=['idle','run','slash','cast']
meta={'skin':skin,'route':'host_image','purpose':'review only; not applied to game','sourceSize':im.size,'columns':4,'rows':4,'actions':{}}
for row,name in enumerate(actions):
    frames=[f.resize((320,293),Image.Resampling.LANCZOS) for f in all_frames[row*4:row*4+4]]
    duration={'idle':[400]*4,'run':[130]*4,'slash':[240,160,120,500],'cast':[300,250,400,400]}[name]
    frames[0].save(out/f'{name}.gif',save_all=True,append_images=frames[1:],duration=duration,loop=0,disposal=2)
    meta['actions'][name]={'frames':4,'durationMs':duration}
(out/'provenance.json').write_text(json.dumps(meta,indent=2),encoding='utf-8')
print(str(out))
