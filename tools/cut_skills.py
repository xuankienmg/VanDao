"""Cắt bảng 'Hỏa Hệ - Bộ kỹ năng & Animation': 8 chiêu chủ động × 8 khung + icon 8 chủ động & 6 bị động.
Dùng: python tools/cut_skills.py <ảnh 1312×1199> <thư mục img>
Xuất: fx_hoa_skills.png (8 hàng × 8 khung, mỗi khung 124×66, giữ nền tối của thẻ) và sk_<id>.png (96×96)."""
import sys
from PIL import Image, ImageDraw
src, out = sys.argv[1], sys.argv[2]
im = Image.open(src).convert('RGB')
ROWS = ['hoa_cau', 'hoa_tien', 'liet_diem', 'hoa_long', 'phan_thien', 'xich_diem_thien', 'hoa_thuan', 'hoa_giap']
TOP = [105, 229, 353, 478, 605, 733, 853, 973]          # mép trên từng hàng thẻ
X0, DX, FW, FH = 248, 132.4, 124, 64
sheet = Image.new('RGB', (FW * 8, FH * 8))
for r, top in enumerate(TOP):
    for c in range(8):
        x = round(X0 + c * DX) + 4
        sheet.paste(im.crop((x, top + 19, x + FW, top + 19 + FH)), (c * FW, r * FH))
sheet.save(f'{out}/fx_hoa_skills.png', optimize=True)

def icon(box, name):
    ic = im.crop(box).resize((96, 96), Image.LANCZOS).convert('RGBA')
    m = Image.new('L', ic.size, 0); ImageDraw.Draw(m).rounded_rectangle([0, 0, 95, 95], 10, fill=255)
    ic.putalpha(m); ic.save(f'{out}/sk_{name}.png', optimize=True)
for r, top in enumerate(TOP):
    icon((24, top + 12, 101, top + 89), ROWS[r])
PASSIVE = ['hoa_tu', 'nhiet_huyet', 'hoa_the', 'liet_diem_tam', 'hoa_linh_chan_y', 'xich_diem_dao_the']
for i, x in enumerate([272, 449, 620, 772, 952, 1127]):
    b = (x, 1102, x + 58, 1160)
    ic = im.crop(b).resize((96, 96), Image.LANCZOS).convert('RGBA')
    m = Image.new('L', ic.size, 0); ImageDraw.Draw(m).ellipse([1, 1, 94, 94], fill=255)
    ic.putalpha(m); ic.save(f'{out}/sk_{PASSIVE[i]}.png', optimize=True)
print('ok')
