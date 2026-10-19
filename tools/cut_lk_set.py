"""Bộ trang bị Thanh Vân (Luyện Khí) từ tools/lk_set_design.png (nền tối): tách từng món trong 10 ô, bỏ nền tối + quầng sáng mờ.
Xuất img/lk_<món>.png (khung 48×48, cùng cỡ ảnh trang bị khác). Dùng: python tools/cut_lk_set.py tools wwwroot/img"""
import sys
import numpy as np
from PIL import Image
from scipy import ndimage
SRC, OUT = sys.argv[1], sys.argv[2]
im = np.array(Image.open(f'{SRC}/lk_set_design.png').convert('RGB')).astype(int)
# (x0, y0, x1, y1), vùng loại trừ (chữ tên món sát món), ngưỡng nền — toạ độ ảnh gốc 1536×1024
BOX = {'mu': ((695, 160, 875, 345), [], 60), 'ao': ((905, 150, 1100, 348), [], 70), 'gang': ((1115, 170, 1305, 342), [], 60),
       'quan': ((1328, 155, 1510, 350), [], 65), 'giay': ((675, 445, 815, 585), [], 65),
       'kiem': ((828, 398, 978, 590), [(828, 398, 958, 422)], 80), 'day': ((1000, 430, 1125, 595), [], 70),
       'nhan': ((1150, 450, 1255, 570), [], 70), 'phu': ((1280, 430, 1390, 595), [], 70), 'thach': ((1405, 440, 1515, 595), [], 105)}
def cut(k):
    (x0, y0, x1, y1), excl, TH = BOX[k]
    c = im[y0:y1, x0:x1].copy()
    cand = c.max(-1) < TH                                              # nền tối + quầng lam mờ
    for ex0, ey0, ex1, ey1 in excl: cand[max(0, ey0 - y0):ey1 - y0, max(0, ex0 - x0):ex1 - x0] = True
    lab, n = ndimage.label(cand)
    border = set(np.unique(np.concatenate([lab[0], lab[-1], lab[:, 0], lab[:, -1]]))) - {0}
    bg = np.isin(lab, list(border))
    for ex0, ey0, ex1, ey1 in excl: bg[max(0, ey0 - y0):ey1 - y0, max(0, ex0 - x0):ex1 - x0] = True
    fg = ndimage.binary_opening(~bg, iterations=1)
    lab, n = ndimage.label(fg); s = ndimage.sum(fg, lab, range(1, n + 1))
    fg = np.isin(lab, [i + 1 for i, v in enumerate(s) if v >= .12 * s.max()])
    fg = ndimage.binary_fill_holes(fg)
    ys, xs = np.nonzero(fg)
    return Image.fromarray(np.dstack([c, fg * 255]).astype(np.uint8)).crop((xs.min(), ys.min(), xs.max() + 1, ys.max() + 1))
def icon(sp, S=48, t=44):
    s = t / max(sp.size); w, h = max(1, round(sp.width * s)), max(1, round(sp.height * s))
    a = np.array(sp).astype(float); al = a[..., 3:] / 255
    p = np.array(Image.fromarray(np.concatenate([a[..., :3] * al, al * 255], -1).astype(np.uint8)).resize((w, h), Image.BOX)).astype(float)
    alpha = p[..., 3]; rgb = np.where(alpha[..., None] > 0, p[..., :3] / np.maximum(alpha[..., None], 1) * 255, 0)
    o = Image.fromarray(np.dstack([np.clip(rgb, 0, 255), np.where(alpha > 110, 255, 0)]).astype(np.uint8))
    can = Image.new('RGBA', (S, S), (0, 0, 0, 0)); can.paste(o, ((S - w) // 2, (S - h) // 2), o); return can
for k in BOX:
    sp = cut(k)
    sp.save(f'{OUT}/big_{k}.png') if len(sys.argv) > 3 else None
    icon(sp).save(f'{OUT}/lk_{k}.png')
print('ok')
