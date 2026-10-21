"""Validate generated game assets without touching them."""
from pathlib import Path
from PIL import Image

root=Path(__file__).resolve().parents[2]/'wwwroot/img/pet'
species=['hoa_linh_ho','bang_tinh_tho','thanh_moc_loc','kim_giap_quy','loi_van_dieu','bich_thuy_giao','thanh_phong_hac','tu_dien_mieu','chu_tuoc','huyen_vu','thanh_long','bach_ho']
checked=0
def check(name,size):
 global checked
 im=Image.open(root/name)
 assert im.size==size,(name,im.size)
 assert im.mode=='RGBA',(name,im.mode)
 alpha=im.getchannel('A')
 assert alpha.getextrema()==(0,255),(name,'missing transparency or opaque subject')
 assert alpha.getbbox(),name
 checked+=1
for animal in species:
 for form in range(3):
  for suffix,size in [('',(320,221)),('_hd',(240,240)),('_48',(48,48)),('_24',(24,24))]:
   check(f'{animal}_{form}{suffix}.png',size)
for item in ['egg_pham','egg_linh','egg_huyen','egg_dia','egg_thien','linh_thu_dan','hoa_hinh_dan','than_thu_huyet','ui_linh_thu']:
 check(f'{item}.png',(64,64))
print(f'PASS {checked} transparent pet and item assets; 12 species, 36 forms, four sizes.')
