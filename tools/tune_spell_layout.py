"""Measure peak art once; keep one stable placement across animation frames."""
import json
from pathlib import Path
from PIL import Image

path=Path('wwwroot/js/water-wood-fx-data.js')
specs=json.loads(path.read_text(encoding='utf-8').split('=',1)[1].strip().rstrip(';'))
ratios={'thuy_cau':.55,'thanh_diep_nhan':.55,'thuy_nguyet':1.0,'van_thuy_tien':1.1,'thuong_hai':1.45,'bach_diep':1.1,'dang_man':1.25,'thuy_lao':1.4,'lam_khoa':1.4,'thien_ha':1.8,'hu_hai':1.5,'hon_nguyen_thuy':1.7,'co_moc':1.6,'thanh_de':1.8,'hu_khong_kien_moc':1.6,'hon_nguyen_moc':1.8}
for s in specs:
    im=Image.open(Path('wwwroot')/s['src']).convert('RGBA')
    boxes=[]
    for i in range(16):
        a=im.crop((i%4*192,i//4*192,i%4*192+192,i//4*192+192)).getchannel('A').point(lambda v:255 if v>=32 else 0)
        if a.getbbox():boxes.append(a.getbbox())
    bounds=[min(b[0] for b in boxes),min(b[1] for b in boxes),max(b[2] for b in boxes),max(b[3] for b in boxes)]
    s['visibleBounds']=bounds;s['targetScale']=ratios[s['skillId']]
    s['anchorX']=(bounds[0]+bounds[2])/2
    if s['skillId'] in ('thuy_lao','lam_khoa'):
        s['motion']='ground';s['anchorY']=bounds[3]
    elif s['motion']=='ground':
        s['anchorY']=bounds[3]
        if s['skillId'] in ('co_moc','thanh_de'):s['anchorX']=140
    else:s['anchorY']=(bounds[1]+bounds[3])/2
path.write_text('const WATER_WOOD_FX_DATA='+json.dumps(specs,ensure_ascii=False)+';\n',encoding='utf-8')
