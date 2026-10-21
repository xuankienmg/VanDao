from pathlib import Path
from PIL import Image
root=Path(__file__).resolve().parents[1]
im=Image.open(root/'tools/pet_art/pet-items.png').convert('RGBA')
names=['egg_pham','egg_linh','egg_huyen','egg_dia','egg_thien','linh_thu_dan','hoa_hinh_dan','than_thu_huyet','ui_linh_thu']
out=root/'wwwroot/img/pet';out.mkdir(parents=True,exist_ok=True)
for i,name in enumerate(names):
 x,y=i%3,i//3
 crop=im.crop((round(x*im.width/3),round(y*im.height/3),round((x+1)*im.width/3),round((y+1)*im.height/3)))
 crop=crop.crop(crop.getchannel('A').getbbox());crop.thumbnail((58,58),Image.Resampling.LANCZOS)
 canvas=Image.new('RGBA',(64,64));canvas.alpha_composite(crop,((64-crop.width)//2,(64-crop.height)//2))
 canvas.save(out/f'{name}.png')
 if name=='ui_linh_thu':canvas.save(root/'wwwroot/img/ui_linh_thu.png')
print('PASS five eggs, three materials and menu icon: 64px RGBA')
