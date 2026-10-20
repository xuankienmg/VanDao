"""Cut generated transparent pet atlases by connected creature components (no background painting)."""
from pathlib import Path
from collections import deque
import json
import numpy as np
from PIL import Image, ImageFilter, ImageChops

ROOT=Path(__file__).resolve().parents[1]
SOURCE=ROOT/'tools/pet_art'
SHEETS=[
 ('pets-design-1.png',['hoa_linh_ho','bang_tinh_tho','thanh_moc_loc']),
 ('pets-design-2.png',['kim_giap_quy','loi_van_dieu','bich_thuy_giao']),
 ('pets-design-3.png',['thanh_phong_hac','tu_dien_mieu','chu_tuoc']),
 ('pets-design-4.png',['huyen_vu','thanh_long','bach_ho']),
]

def cut(filename,species):
 im=Image.open(SOURCE/filename).convert('RGBA');small=im.getchannel('A').resize((im.width//2,im.height//2),Image.Resampling.NEAREST)
 mask=np.array(small)>96;labels=np.zeros(mask.shape,dtype=np.int32);height,width=mask.shape;parts=[];label=0
 for y,x in np.argwhere(mask):
  if labels[y,x]: continue
  label+=1;labels[y,x]=label;q=deque([(int(x),int(y))]);count=0;left=right=int(x);top=bottom=int(y)
  while q:
   xx,yy=q.popleft();count+=1;left=min(left,xx);right=max(right,xx);top=min(top,yy);bottom=max(bottom,yy)
   for nx,ny in ((xx-1,yy),(xx+1,yy),(xx,yy-1),(xx,yy+1)):
    if 0<=nx<width and 0<=ny<height and mask[ny,nx] and not labels[ny,nx]:labels[ny,nx]=label;q.append((nx,ny))
  if count>1000:parts.append((count,label,(left,top,right+1,bottom+1)))
 parts=sorted(parts,reverse=True)[:9]
 if len(parts)!=9:raise ValueError(f'{filename}: expected 9 creatures, got {len(parts)}')
 parts.sort(key=lambda p:(p[2][1]+p[2][3])/2)
 out=ROOT/'wwwroot/img/pet';out.mkdir(parents=True,exist_ok=True)
 for row in range(3):
  group=sorted(parts[row*3:row*3+3],key=lambda p:p[2][0])
  for form,(_,tag,bbox) in enumerate(group):
   matte=Image.fromarray(np.uint8(labels==tag)*255).resize(im.size,Image.Resampling.NEAREST).filter(ImageFilter.MaxFilter(7))
   alpha=ImageChops.multiply(im.getchannel('A'),matte);crop=im.copy();crop.putalpha(alpha);crop=crop.crop(alpha.getbbox())
   for suffix,size in [('',(320,221)),('_hd',(240,240)),('_48',(48,48)),('_24',(24,24))]:
    canvas=Image.new('RGBA',size);factor=min(size[0]*.92/crop.width,size[1]*(.66,.78,.89)[form]/crop.height)
    scaled=crop.resize((max(1,round(crop.width*factor)),max(1,round(crop.height*factor))),Image.Resampling.LANCZOS)
    canvas.alpha_composite(scaled,((size[0]-scaled.width)//2,round(size[1]*.94)-scaled.height))
    canvas.save(out/f'{species[row]}_{form}{suffix}.png')
 print('PASS cut',filename,'->',','.join(species),'3 forms × 4 sizes')

if __name__=='__main__':
 for file,species in SHEETS:cut(file,species)
