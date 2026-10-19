"""Bộ trang bị Huyền Long (Luyện Hư) từ tools/lh_set_design.png (nền tối): tách từng món ở các ô lớn, bỏ nền tối + quầng sáng mờ.
Xuất img/lh_<món>.png (khung 48×48, cùng cỡ ảnh trang bị khác). Dùng: python tools/cut_lh_set.py tools wwwroot/img"""
import sys
import numpy as np
from PIL import Image
from scipy import ndimage
SRC, OUT = sys.argv[1], sys.argv[2]
im = np.array(Image.open(f'{SRC}/lh_set_design.png').convert('RGB')).astype(int)
# (x0, y0, x1, y1), vùng loại trừ (khung icon đỏ / món phụ cạnh bên) tính theo toạ độ ảnh gốc
BOX = {'mu': ((818, 40, 990, 200), []), 'ao': ((1182, 34, 1345, 218), []), 'gang': ((818, 262, 1050, 440), []),
       'quan': ((1182, 262, 1318, 442), []), 'giay': ((818, 486, 1018, 676), []), 'truong': ((1255, 478, 1400, 676), []),
       'day': ((668, 722, 776, 960), []), 'nhan': ((880, 725, 1060, 892), []), 'phu': ((1090, 722, 1216, 960), []),
       'kinh': ((1315, 718, 1500, 962), [(1440, 870, 1520, 962), (1478, 718, 1520, 870)])}
TH = 70
def cut(k):
    (x0, y0, x1, y1), excl = BOX[k]
    c = im[y0:y1, x0:x1].copy()
    mx = c.max(-1)
    cand = mx < TH                                                     # nền tối + quầng lam mờ
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
    icon(sp).save(f'{OUT}/lh_{k}.png')
print('ok')
