"""Install the female Hợp Thể skin from keyed standing, meditation and action art."""
import sys,json
from pathlib import Path
import numpy as np
from PIL import Image

root=Path(__file__).resolve().parents[1]; out=root/'wwwroot/img'; skin=out/'skin'
review=root/'docs/skin-demos/hopthe-nu';review.mkdir(parents=True,exist_ok=True)
def keyed(path):
    a=np.array(Image.open(path).convert('RGBA'));r=a[:,:,:3].astype(int)
    key=(r[:,:,1]>100)&(r[:,:,1]>r[:,:,0]+45)&(r[:,:,1]>r[:,:,2]+45)
    a[key]=0
    spill=(a[:,:,3]>0)&(r[:,:,1]>r[:,:,0]+10)&(r[:,:,1]>r[:,:,2]+10)
    a[:,:,1][spill]=np.maximum(a[:,:,0],a[:,:,2])[spill]
    return Image.fromarray(a)
def crop(im):return im.crop(im.getbbox())
def fit(im,h):return im.resize((max(1,round(im.width*h/im.height)),h),Image.Resampling.LANCZOS)
stand=crop(keyed(sys.argv[1]));med=crop(keyed(sys.argv[2]));sheet=keyed(sys.argv[3])
stand.save(review/'standing-cutout.png');med.save(review/'meditation-cutout.png');sheet.save(review/'action-source.png')
a=np.array(sheet);mask=a[:,:,3]>32;seen=np.zeros(mask.shape,bool);h,w=mask.shape;groups=[[] for _ in range(16)]
for yy,xx in zip(*np.nonzero(mask)):
    if seen[yy,xx]:continue
    todo=[(xx,yy)];pts=[];seen[yy,xx]=True
    while todo:
        x,y=todo.pop();pts.append((x,y))
        for dx,dy in ((-1,-1),(0,-1),(1,-1),(-1,0),(1,0),(-1,1),(0,1),(1,1)):
            nx,ny=x+dx,y+dy
            if 0<=nx<w and 0<=ny<h and mask[ny,nx] and not seen[ny,nx]:
                seen[ny,nx]=True;todo.append((nx,ny))
    if len(pts)<250:continue
    cx=sum(x for x,y in pts)/len(pts);cy=sum(y for x,y in pts)/len(pts)
    groups[min(3,int(cy*4/h))*4+min(3,int(cx*4/w))].extend(pts)
poses=[]
for i,pts in enumerate(groups):
    if not pts:raise ValueError(f'Empty pose {i}')
    xs,ys=zip(*pts);l,r,t,b=min(xs),max(xs)+1,min(ys),max(ys)+1
    data=np.zeros((b-t,r-l,4),dtype=np.uint8)
    for x,y in pts:data[y-t,x-l]=a[y,x]
    feet=[x for x,y in pts if y>=b-28]
    poses.append((Image.fromarray(data),(min(feet)+max(feet))/2-l))
body=round(sum(f.height for f,ax in poses[:4])/4)
# Fit the whole family with one scale, retaining extended blades in their cells.
factor=min(1,360/body,min(410/f.height for f,ax in poses),min(232/max(ax,1) for f,ax in poses),min(232/max(f.width-ax,1) for f,ax in poses))
target=round(body*factor);frames=[]
for fig,ax in poses:
    fig=fig.resize((round(fig.width*factor),round(fig.height*factor)),Image.Resampling.LANCZOS)
    cell=Image.new('RGBA',(480,440));cell.alpha_composite(fig,(round(240-ax*factor),420-fig.height));frames.append(cell)
atlas=Image.new('RGBA',(1920,1760))
for i,f in enumerate(frames):atlas.alpha_composite(f,((i%4)*480,(i//4)*440))
atlas.save(skin/'hopthe_nu_actions.png')
portrait=fit(stand,600);portrait.save(out/'hero_hopthe_nu_stand.png');fit(stand,360).save(out/'hero_hopthe_nu.png');fit(med,600).save(out/'med_hopthe_nu.png')
for size in (24,48):
    ratio=min((size-2)/stand.width,(size-2)/stand.height)
    fig=stand.resize((round(stand.width*ratio),round(stand.height*ratio)),Image.Resampling.LANCZOS)
    icon=Image.new('RGBA',(size,size));icon.alpha_composite(fig,((size-fig.width)//2,size-1-fig.height));icon.save(out/f'hero_hopthe_nu_{size}.png')
# The supplied standing illustration is also the fixed-leg airborne pose and
# the stable inventory idle source, so the original face remains recognisable.
fig=fit(stand,target)
footbox=stand.getchannel('A').point(lambda v:255 if v>=128 else 0).crop((0,max(0,stand.height-40),stand.width,stand.height)).getbbox()
footx=(footbox[0]+footbox[2])/2
flight=Image.new('RGBA',(480,440));flight.alpha_composite(fig,(round(240-footx*target/stand.height),420-fig.height))
flight.save(skin/'hopthe_nu_idle.png')
flight_atlas=Image.new('RGBA',(1920,440))
for i in range(4):flight_atlas.alpha_composite(flight,(i*480,0))
flight_atlas.save(skin/'hopthe_nu_flight.png')
medfactor=min(460/med.width,410/med.height);fig=med.resize((round(med.width*medfactor),round(med.height*medfactor)),Image.Resampling.LANCZOS)
medcell=Image.new('RGBA',(480,440));medcell.alpha_composite(fig,((480-fig.width)//2,420-fig.height));medcell.save(skin/'hopthe_nu_meditate.png')
ref=Image.open(out/'hoa_cau_ref_hopthe.png').convert('RGBA')
feet=[(76,153),(70,150),(38,148),(33,146),None,None,None,(33,146)];scales=[1,1.107,1.069,1.204,1,1,1,1.204]
for i in (0,1,2,3,7):
    ref.paste((0,0,0,0),(i*168,0,(i+1)*168,158));fig=fit(crop(frames[12+min(3,i if i<4 else 3)]),round(124/scales[i]))
    layer=Image.new('RGBA',(168,158));fx,fy=feet[i];layer.alpha_composite(fig,(fx-fig.width//2,fy-fig.height));ref.alpha_composite(layer,(i*168,0))
ref.save(out/'hoa_cau_ref_hopthe_nu.png')
meta={'id':'hop_the_nu','name':'Thiên Hợp Tiên Cơ','minRealm':6,'route':'host_image','model':None,'estimateUsd':None,'bodyHeight':target,'frameWidth':480,'frameHeight':440,'anchor':[240,420],'sourceScale':factor,'rows':{'idle':0,'groundRun':1,'slash':2,'cast':3},'inventorySrc':'img/skin/hopthe_nu_idle.png','meditate':'img/med_hopthe_nu.png','flight':'fixed standing pose with runtime levitation and foot effect'}
(skin/'hopthe_nu_actions.json').write_text(json.dumps(meta,ensure_ascii=False,indent=2),encoding='utf-8')
print(json.dumps(meta,ensure_ascii=False))
