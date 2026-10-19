"""Publish approved review sheets as runtime assets without regenerating artwork."""
import json
from pathlib import Path
from PIL import Image

root=Path(__file__).resolve().parents[1]
out=root/'wwwroot/img/skin'
profiles={}
for skin in ('trucco','kimdan','luyenhu'):
    src=root/'docs/skin-demos'/skin
    im=Image.open(src/'sheet.png').convert('RGBA')
    assert im.size==(1920,1760)
    heights=[]
    for col in range(4):
        a=im.crop((col*480,0,(col+1)*480,440)).getchannel('A')
        b=a.point(lambda v:255 if v>=128 else 0).getbbox()
        heights.append(b[3]-b[1])
    height=round(sum(heights)/len(heights))
    im.save(out/f'{skin}_actions.png')
    profile={'src':f'img/skin/{skin}_actions.png','frameWidth':480,'frameHeight':440,'anchor':[240,420],'bodyHeight':height,'columns':4,'rows':{'idle':0,'run':1,'slash':2,'cast':3},'framesPerAction':4,'provenance':json.loads((src/'provenance.json').read_text())}
    (out/f'{skin}_actions.json').write_text(json.dumps(profile,indent=2),encoding='utf-8')
    profiles[skin]=height
print(json.dumps(profiles))
