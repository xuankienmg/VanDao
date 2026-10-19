"""Pack a four-pose flight sheet at each skin's established actor scale."""
import sys,json
from pathlib import Path
import numpy as np
from PIL import Image

skin,source=sys.argv[1:3]
root=Path(__file__).resolve().parents[1]
out=root/'wwwroot/img/skin'
im=Image.open(source).convert('RGBA')
im.save(out/f'{skin}_flight_source.png')
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
# Partition by original cells, preserving detached physical sword components.
groups=[[] for _ in range(4)]
for pts in parts:
    cx=sum(x for x,y in pts)/len(pts); cy=sum(y for x,y in pts)/len(pts)
    groups[(2 if cy>=h/2 else 0)+(1 if cx>=w/2 else 0)].extend(pts)
if any(not group for group in groups): raise ValueError('Expected a flight silhouette in every 2x2 cell')
profile=json.loads((out/f'{skin}_actions.json').read_text())
target=profile['bodyHeight']
# One scale for the whole loop; flight sword remains below the body.
first=groups[0]; first_height=max(y for x,y in first)-min(y for x,y in first)+1
def blade_anchor(pts):
    top=min(y for x,y in pts); bottom=max(y for x,y in pts)
    width=max(x for x,y in pts)-min(x for x,y in pts)
    rows={}
    for x,y in pts: rows.setdefault(y,[]).append(x)
    candidates=[y for y,xs in rows.items() if y>top+(bottom-top)*.65 and max(xs)-min(xs)>width*.6]
    return max(candidates) if candidates else bottom+1
first_baseline=blade_anchor(first) if skin!='luyenhu' else max(y for x,y in first)+1
scale=target/(first_baseline-min(y for x,y in first))
anchor_y=420 if skin=='luyenhu' else 380
frames=[]
for pts in groups:
    xs,ys=zip(*pts); l,r,t,b=min(xs),max(xs)+1,min(ys),max(ys)+1
    owned=np.zeros((b-t,r-l,4),dtype=np.uint8)
    for x,y in pts: owned[y-t,x-l]=arr[y,x]
    fig=Image.fromarray(owned)
    fig=fig.resize((round(fig.width*scale),round(fig.height*scale)),Image.Resampling.LANCZOS)
    if fig.width>470 or fig.height>420: raise ValueError('Flight exceeds cell safe area')
    frame=Image.new('RGBA',(480,440))
    # Fixed original cell center and bottom registration; runtime adds levitation.
    baseline=blade_anchor(pts) if skin!='luyenhu' else b
    feet=[x for x,y in pts if baseline-45<=y<baseline-10]
    anchor=(min(feet)+max(feet))/2 if feet else (l+r)/2
    x=round(240-(anchor-l)*scale)
    if x<0 or x+fig.width>480: x=(480-fig.width)//2
    frame.alpha_composite(fig,(x,round(anchor_y-(baseline-t)*scale)))
    frames.append(frame)
atlas=Image.new('RGBA',(1920,440))
for i,f in enumerate(frames): atlas.alpha_composite(f,(i*480,0))
atlas.save(out/f'{skin}_flight.png')
frames[0].save(out/f'{skin}_flight.gif',save_all=True,append_images=frames[1:],duration=240,loop=0,disposal=2)
meta={'skin':skin,'motion':'air-walk' if skin=='luyenhu' else 'sword-flight','route':'host_image','frames':4,'size':[480,440],'anchor':[240,anchor_y],'bodyHeight':target,'scale':scale}
(out/f'{skin}_flight.json').write_text(json.dumps(meta,indent=2),encoding='utf-8')
print(json.dumps(meta))
