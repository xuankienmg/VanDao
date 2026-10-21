"""Key and install the replacement Hợp Thể seated illustration, including halo."""
import sys,json,shutil
from pathlib import Path
import numpy as np
from PIL import Image

root=Path(__file__).resolve().parents[1]
out=root/'wwwroot/img'; review=root/'docs/skin-demos/hopthe/meditation-v2'
review.mkdir(parents=True,exist_ok=True)
for name,p in [('meditation-v1.png',out/'med_hopthe.png'),('atlas-v1.png',out/'skin/hopthe_meditate.png')]:
    backup=review/name
    if p.exists() and not backup.exists(): shutil.copy2(p,backup)
im=Image.open(sys.argv[1]).convert('RGBA')
im.save(review/'keyed-source.png')
a=np.array(im); rgb=a[:,:,:3].astype(int)
key=(rgb[:,:,1]>100)&(rgb[:,:,1]>rgb[:,:,0]+45)&(rgb[:,:,1]>rgb[:,:,2]+45)
a[key]=0
# Suppress residual green spill only on green-dominant antialiased edges.
spill=(a[:,:,3]>0)&(rgb[:,:,1]>rgb[:,:,0]+10)&(rgb[:,:,1]>rgb[:,:,2]+10)
a[:,:,1][spill]=np.maximum(a[:,:,0],a[:,:,2])[spill]
im=Image.fromarray(a); box=im.getbbox()
if not box: raise ValueError('Empty keyed illustration')
im=im.crop(box)
im.save(review/'meditation-cutout.png')
portrait=im.resize((round(im.width*600/im.height),600),Image.Resampling.LANCZOS)
portrait.save(out/'med_hopthe.png')
factor=min(460/im.width,410/im.height)
fig=im.resize((round(im.width*factor),round(im.height*factor)),Image.Resampling.LANCZOS)
cell=Image.new('RGBA',(480,440));cell.alpha_composite(fig,((480-fig.width)//2,420-fig.height))
# This is a supplied still; the game adds breathing rather than cycling poses.
atlas=Image.new('RGBA',(1920,440))
for i in range(4): atlas.alpha_composite(cell,(i*480,0))
atlas.save(out/'skin/hopthe_meditate.png')
meta={'source':'user-supplied frontal seated illustration with golden halo','route':'host_image edit; deterministic green key','poseCount':1,'animation':'existing runtime breathing','portrait':'img/med_hopthe.png','atlas':'img/skin/hopthe_meditate.png','halo':'preserved','portraitSize':portrait.size}
(review/'provenance.json').write_text(json.dumps(meta,indent=2),encoding='utf-8')
p=out/'skin/hopthe_actions.json'; action=json.loads(p.read_text());action['meditationRevision']=meta;p.write_text(json.dumps(action,indent=2),encoding='utf-8')
preview=Image.new('RGBA',(portrait.width,portrait.height),(24,34,45,255));preview.alpha_composite(portrait);preview.convert('RGB').save(review/'preview.png')
print(json.dumps(meta))
