"""Encode browser-rendered frame captures as GIFs; does not generate artwork."""
from pathlib import Path
from PIL import Image

root=Path(__file__).resolve().parents[2]
durations=[138,137,138,137,110,110,110,110,83,82,83,82,83,82,83,82,500]
for skill in ('hoa_cau','liet_diem','hoa_long','phan_thien','xich_diem_thien','than_hoa','hu_khong_hoa'):
    paths=sorted((root/'obj/fire-frames').glob(f'{skill}-*.png'))
    if len(paths)!=17:
        raise ValueError(f'{skill}: expected 17 captured frames, got {len(paths)}')
    frames=[Image.open(p).convert('RGB') for p in paths]
    output=root/f'obj/fire-{skill}.gif'
    frames[0].save(output,save_all=True,append_images=frames[1:],duration=durations,loop=0,optimize=False)
    for frame in frames: frame.close()
    print(output)
