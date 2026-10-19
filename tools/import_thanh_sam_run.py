"""Import the supplied six-pose run sheet; retain one scale across all frames."""
import sys
from pathlib import Path
from collections import deque
import numpy as np
from PIL import Image

src = Image.open(sys.argv[1]).convert('RGBA')
out = Path(__file__).resolve().parents[1] / 'wwwroot/img/skin'
out.mkdir(parents=True, exist_ok=True)
src.save(out / 'luyenkhi_run_source.png')
frames = []
for left, right in [(15,180),(185,355),(365,535),(535,705),(710,850),(850,1024)]:
    a = np.array(src.crop((left,130,right,390)))
    rgb = a[:,:,:3].astype(int)
    bg = (rgb.max(2)-rgb.min(2)<28) & (rgb.min(2)>95)
    h,w = bg.shape
    seen = np.zeros((h,w),bool)
    q = deque([(x,y) for y in range(h) for x in (0,w-1)] + [(x,y) for x in range(w) for y in (0,h-1)])
    while q:
        x,y=q.popleft()
        if not (0<=x<w and 0<=y<h) or seen[y,x] or not bg[y,x]: continue
        seen[y,x]=True
        q.extend(((x-1,y),(x+1,y),(x,y-1),(x,y+1)))
    a[seen]=0
    # Cyan dust outside the body is excluded from the supplied source.
    a[(rgb[:,:,1]>rgb[:,:,0]+12) & (rgb[:,:,2]>rgb[:,:,0]+12) & (rgb.min(2)>95)]=0
    # Drop isolated checkerboard-edge specks, preserving all substantial pieces.
    visited=np.zeros((h,w),bool)
    for yy,xx in zip(*np.nonzero(a[:,:,3])):
        if visited[yy,xx]: continue
        todo=[(xx,yy)]; component=[]; visited[yy,xx]=True
        while todo:
            x,y=todo.pop(); component.append((x,y))
            for nx,ny in ((x-1,y),(x+1,y),(x,y-1),(x,y+1)):
                if 0<=nx<w and 0<=ny<h and not visited[ny,nx] and a[ny,nx,3]:
                    visited[ny,nx]=True; todo.append((nx,ny))
        if len(component)<30:
            for x,y in component: a[y,x]=0
    im=Image.fromarray(a)
    box=im.getbbox()
    im=im.crop(box)
    # Fixed source scale, centered horizontally, shared ground baseline.
    im=im.resize((round(im.width*.72),round(im.height*.72)),Image.Resampling.LANCZOS)
    frame=Image.new('RGBA',(160,192))
    frame.alpha_composite(im,((160-im.width)//2,188-im.height))
    frames.append(frame)
sheet=Image.new('RGBA',(960,192))
for i,frame in enumerate(frames): sheet.alpha_composite(frame,(i*160,0))
sheet.save(out/'luyenkhi_run.png')
frames[0].save(out/'luyenkhi_run_preview.gif',save_all=True,append_images=frames[1:],duration=100,loop=0,disposal=2)
print('Imported 6 frames, 160x192, 10 fps; shared scale 0.72; alpha background removed')
