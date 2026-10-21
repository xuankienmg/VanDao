"""Package Hợp Thể identity/action art for the existing game renderers."""
import sys,json
from pathlib import Path
import numpy as np
from PIL import Image

root=Path(__file__).resolve().parents[1]; out=root/'wwwroot/img'; skin=out/'skin'
review=root/'docs/skin-demos/hopthe'; review.mkdir(parents=True,exist_ok=True)
def extract(path,rows,cols):
    im=Image.open(path).convert('RGBA'); a=np.array(im)
    # Optional flat-green key for white-haired/white-robed art. Real blue/gold
    # costume colours are protected by requiring green to dominate both channels.
    rgb=a[:,:,:3].astype(int)
    key=(rgb[:,:,1]>100)&(rgb[:,:,1]>rgb[:,:,0]+45)&(rgb[:,:,1]>rgb[:,:,2]+45)
    a[key]=0
    im=Image.fromarray(a)
    mask=a[:,:,3]>32; seen=np.zeros(mask.shape,bool); h,w=mask.shape
    groups=[[] for _ in range(rows*cols)]
    for yy,xx in zip(*np.nonzero(mask)):
        if seen[yy,xx]: continue
        todo=[(xx,yy)]; pts=[]; seen[yy,xx]=True
        while todo:
            x,y=todo.pop(); pts.append((x,y))
            for dx,dy in ((-1,-1),(0,-1),(1,-1),(-1,0),(1,0),(-1,1),(0,1),(1,1)):
                nx,ny=x+dx,y+dy
                if 0<=nx<w and 0<=ny<h and mask[ny,nx] and not seen[ny,nx]:
                    seen[ny,nx]=True; todo.append((nx,ny))
        if len(pts)<500: continue
        cx=sum(x for x,y in pts)/len(pts); cy=sum(y for x,y in pts)/len(pts)
        groups[min(rows-1,int(cy*rows/h))*cols+min(cols-1,int(cx*cols/w))].extend(pts)
    figures=[]
    for i,pts in enumerate(groups):
        if not pts: raise ValueError(f'Empty pose {i}')
        xs,ys=zip(*pts); l,r,t,b=min(xs),max(xs)+1,min(ys),max(ys)+1
        fig=np.zeros((b-t,r-l,4),dtype=np.uint8)
        for x,y in pts: fig[y-t,x-l]=a[y,x]
        feet=[x for x,y in pts if y>=b-30]
        figures.append((Image.fromarray(fig),(min(feet)+max(feet))/2-l))
    return im,figures

main,poses=extract(sys.argv[1],4,4); combat,attacks=extract(sys.argv[2],2,4)
main.save(review/'poses-source.png');combat.save(review/'combat-source.png')
body=round(sum(p[0].height for p in poses[:4])/4)
scale=min(1,360/body); target=round(body*scale)
def pack(figs,factor):
    frames=[]
    for fig,ax in figs:
        fig=fig.resize((round(fig.width*factor),round(fig.height*factor)),Image.Resampling.LANCZOS)
        x=round(240-ax*factor); y=420-fig.height
        if x<5 or x+fig.width>475 or y<5:
            raise ValueError(f'Pose exceeds safe frame: {fig.size}, root {ax}, xy {x,y}')
        frame=Image.new('RGBA',(480,440));frame.alpha_composite(fig,(x,y));frames.append(frame)
    return frames
base=pack(poses,scale)
combat_scale=target/attacks[4][0].height
attack=pack(attacks,combat_scale)
def atlas(frames,path,cols=4):
    image=Image.new('RGBA',(480*cols,440*((len(frames)+cols-1)//cols)))
    for i,f in enumerate(frames): image.alpha_composite(f,((i%cols)*480,(i//cols)*440))
    image.save(path)
atlas(base[:8]+attack,skin/'hopthe_actions.png')
atlas(base[12:16],skin/'hopthe_flight.png')
atlas(base[8:12],skin/'hopthe_meditate.png')
atlas(base,review/'poses.png')
for name,frames,ms in [('stand',base[:4],300),('run',base[4:8],130),('meditate',base[8:12],400),('flight',base[12:16],300)]:
    frames[0].save(review/f'{name}.gif',save_all=True,append_images=frames[1:],duration=ms,loop=0,disposal=2)
def cropped(im): return im.crop(im.getbbox())
def fit(im,height):
    return im.resize((round(im.width*height/im.height),height),Image.Resampling.LANCZOS)
standing=cropped(base[0]);med=cropped(base[8])
fit(standing,360).save(out/'hero_hopthe.png');fit(standing,360).save(out/'hero_hopthe_stand.png')
fit(med,400).save(out/'med_hopthe.png')
for size in (24,48):
    ratio=min((size-2)/standing.width,(size-2)/standing.height)
    fig=standing.resize((round(standing.width*ratio),round(standing.height*ratio)),Image.Resampling.LANCZOS)
    icon=Image.new('RGBA',(size,size));icon.alpha_composite(fig,((size-fig.width)//2,size-1-fig.height));icon.save(out/f'hero_hopthe_{size}.png')
# Legacy effect positions remain intact; all character slots use the new skin.
ref=Image.open(out/'hoa_cau_ref_luyenhu.png').convert('RGBA')
feet=[(76,153),(70,150),(38,148),(33,146),None,None,None,(33,146)]
scales=[1,1.107,1.069,1.204,1,1,1,1.204]
for i in (0,1,2,3,7):
    ref.paste((0,0,0,0),(i*168,0,(i+1)*168,158))
    fig=fit(cropped(attack[4+min(3,i if i<4 else 3)]),round(124/scales[i]))
    layer=Image.new('RGBA',(168,158));fx,fy=feet[i]
    layer.alpha_composite(fig,(fx-fig.width//2,fy-fig.height));ref.alpha_composite(layer,(i*168,0))
ref.save(out/'hoa_cau_ref_hopthe.png')
meta={'id':'hop_the','route':'host_image','model':None,'estimateUsd':None,'bodyHeight':target,'frameWidth':480,'frameHeight':440,'anchor':[240,420],'rows':{'idle':0,'groundRun':1,'slash':2,'cast':3},'framesPerAction':4,'meditate':'img/skin/hopthe_meditate.png','flight':'img/skin/hopthe_flight.png','sourceScale':scale,'combatScale':combat_scale}
(skin/'hopthe_actions.json').write_text(json.dumps(meta,indent=2),encoding='utf-8')
print(json.dumps(meta))
