"""Prepare the existing Thanh Sam breeze sheet for the inventory idle renderer."""
from pathlib import Path
from PIL import Image

root=Path(__file__).resolve().parents[1]
im=Image.open(root/'tools/art-sources/skin/luyenkhi_idle_breeze_sheet.png').convert('RGBA')
frames=[]
scale=None
for row in range(2):
    for col in range(4):
        frame=im.crop((round(col*im.width/4),round(row*im.height/2),round((col+1)*im.width/4),round((row+1)*im.height/2)))
        a=frame.getchannel('A').point(lambda v:255 if v>=128 else 0)
        box=a.getbbox()
        if not box: raise ValueError('Empty idle pose')
        if scale is None: scale=210/(box[3]-box[1])
        feet=a.crop((0,box[3]-40,frame.width,box[3])).getbbox()
        anchor=(feet[0]+feet[2])/2
        crop=frame.crop(box)
        crop=crop.resize((round(crop.width*scale),round(crop.height*scale)),Image.Resampling.LANCZOS)
        cell=Image.new('RGBA',(384,320))
        cell.alpha_composite(crop,(round(192-(anchor-box[0])*scale),308-crop.height))
        frames.append(cell)
atlas=Image.new('RGBA',(384*8,320))
for i,frame in enumerate(frames): atlas.alpha_composite(frame,(i*384,0))
atlas.save(root/'wwwroot/img/skin/luyenkhi_inventory_breeze.png')
print('Packed 8 idle frames, 384x320, shared scale and foot anchor')
