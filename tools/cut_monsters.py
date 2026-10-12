"""Cắt 7 quái thường + Boss Luyện Khí (Hắc Lân Mãng) từ bảng thiết kế quái (nền ô cờ giả trong suốt).
Tách nền: ô cờ đen/xám trung tính, phẳng, loang từ mép khung + túi ô cờ kẹt bên trong; bóc viền dính ô cờ,
rồi kẻ viền tối 1px. Xuất <id>.png 320×221 (quay phải), <id>_48.png / _24.png (quay trái, đáy sát mép) và
<id>_hd.png 240×240 (cùng bố cục _48, giữ nét ảnh gốc — trận đấu vẽ bản này ở độ phân giải thật).
Dùng: python tools/cut_monsters.py tools/monsters_design.png wwwroot/img"""
import numpy as np, sys
from PIL import Image
from scipy import ndimage
import sys
SRC, OUTDIR = sys.argv[1], sys.argv[2]
a = np.array(Image.open(SRC).convert('RGB')).astype(int)
BOX = {
 'quai_yeu_lang':   (36, 40, 352, 302),
 'quai_doc_xa':     (372, 40, 694, 302),
 'quai_thach_quy':  (715, 40, 1037, 302),
 'quai_hoa_ho':     (1058, 40, 1372, 302),
 'quai_bang_lang':  (36, 400, 352, 668),
 'quai_thiet_trung':(372, 400, 694, 668),
 'quai_cot_khuyen': (715, 400, 1037, 668),
 'boss_hac_lan':    (1058, 394, 1372, 668),
}
FLAT, NEU, POCKET, PEEL = 3.0, 11, 20, 4
SEEDS = {   # điểm nền kẹt (ô cờ bị tối đi) cần xóa tay: (x, y) trong khung cắt
  'quai_doc_xa': [(113, 193), (100, 190), (128, 192)],
  'quai_hoa_ho': [(279, 163), (286, 143), (272, 175), (270, 185), (262, 178)],
  'quai_cot_khuyen': [(145, 200), (130, 175), (150, 215)],
}
CLEAR = {'boss_hac_lan': [(229, 258, 22), (72, 22, 9)], 'quai_doc_xa': [(126, 104, 8)], 'quai_thach_quy': [(302, 240, 8)]}   # (x, y, bán kính): dấu sao lấp lánh
def mask(c, k):
    v = c.mean(-1); neu = (c.max(-1) - c.min(-1)) <= NEU
    cls = neu & ((v <= 15) | ((v >= 42) & (v <= 78)))
    sd = np.sqrt(ndimage.uniform_filter(v**2, 5) - ndimage.uniform_filter(v, 5)**2)
    seed = cls & (sd < FLAT)
    near = ndimage.distance_transform_edt(~seed) <= 3
    cand = neu & (v <= 82) & near
    lab, n = ndimage.label(cand)
    border = set(np.unique(np.concatenate([lab[0], lab[-1], lab[:, 0], lab[:, -1]]))) - {0}
    # also any component with lots of flat seed (enclosed checker pockets): seed fraction high
    keep = set(border)
    for i, sl in enumerate(ndimage.find_objects(lab), 1):
        if i in keep: continue
        m = lab[sl] == i
        if m.sum() > 60 and seed[sl][m].mean() > .5: keep.add(i)
    bg = np.isin(lab, list(keep))
    # túi nền kẹt bên trong (vòng rắn, kẽ chân): vùng ô cờ phẳng đủ lớn
    grp = ndimage.binary_dilation(seed, iterations=2) & neu & (v <= 82)
    sl, sn = ndimage.label(grp)
    dark, light = seed & (v <= 15), seed & (v >= 42)
    for i, box in enumerate(ndimage.find_objects(sl), 1):
        m = sl[box] == i
        d, l = (dark[box] & m).sum(), (light[box] & m).sum()
        if d >= POCKET and l >= POCKET and min(d, l) / max(d, l) > .25:   # ô cờ thật: có cả ô đen lẫn ô xám
            bg[box] |= m
    for (sx, sy) in SEEDS.get(k, []):
        yy, xx = np.ogrid[:c.shape[0], :c.shape[1]]
        near_pt = (xx - sx) ** 2 + (yy - sy) ** 2 <= 45 ** 2
        reg, _ = ndimage.label(neu & (v <= 60) & near_pt)
        if reg[sy, sx]: bg |= reg == reg[sy, sx]
    for (cx, cy, r) in CLEAR.get(k, []):
        yy, xx = np.ogrid[:c.shape[0], :c.shape[1]]
        bg |= (xx - cx) ** 2 + (yy - cy) ** 2 <= r * r
    fg = ~bg
    # bóc viền: điểm đen / xám trung tính sát nền (ô cờ dính vào viền) — bóc vài lớp
    for _ in range(PEEL):
        edge = fg & ~ndimage.binary_erosion(fg)
        rm = edge & neu & ((v <= 22) | ((v >= 40) & (v <= 80)))
        if not rm.any(): break
        fg &= ~rm
    fg = ndimage.binary_opening(fg, iterations=1)
    lab2, n2 = ndimage.label(fg)
    if n2:
        sizes = ndimage.sum(fg, lab2, range(1, n2 + 1))
        fg = np.isin(lab2, [i + 1 for i, s in enumerate(sizes) if s >= max(200, sizes.max() * .06)])
    return fg
RAW = {}
for k, (x0, y0, x1, y1) in BOX.items():
    c = a[y0:y1, x0:x1]; fg = mask(c, k)
    RAW[k] = Image.fromarray(np.dstack([c, fg * 255]).astype(np.uint8))

OL = np.array([11, 8, 6])
def crop(im):
    a = np.array(im); ys, xs = np.nonzero(a[..., 3] > 0)
    return im.crop((xs.min(), ys.min(), xs.max() + 1, ys.max() + 1))
def resize_pm(im, w, h):
    a = np.array(im).astype(float); al = a[..., 3:] / 255
    pm = np.concatenate([a[..., :3] * al, al * 255], -1)
    r = np.array(Image.fromarray(pm.astype(np.uint8)).resize((w, h), Image.BOX)).astype(float)   # 8-bit pm ok
    al2 = r[..., 3:] / 255
    rgb = np.where(al2 > 0, r[..., :3] / np.maximum(al2, 1e-6), 0)
    return np.clip(rgb, 0, 255), al2[..., 0]
def outline(rgb, mask):
    ring = ndimage.binary_dilation(mask) & ~mask
    rgb = rgb.copy(); rgb[ring] = OL
    return rgb, mask | ring
def place(k, W, H, mirror, pad):
    sp = crop(RAW[k])
    if mirror: sp = sp.transpose(Image.FLIP_LEFT_RIGHT)
    s = min((W - 2 * pad) / sp.width, (H - 2 * pad) / sp.height)
    w, h = max(1, round(sp.width * s)), max(1, round(sp.height * s))
    rgb, al = resize_pm(sp, w, h)
    m = al >= .5
    canvas = np.zeros((H, W, 3)); cm = np.zeros((H, W), bool)
    x0 = (W - w) // 2; y0 = H - pad - h
    canvas[y0:y0+h, x0:x0+w][m] = rgb[m]; cm[y0:y0+h, x0:x0+w] = m
    canvas, cm = outline(canvas, cm)
    return Image.fromarray(np.dstack([canvas, cm * 255]).astype(np.uint8))
for k in BOX:
    place(k, 320, 221, False, 2).save(f'{OUTDIR}/{k}.png')
    place(k, 48, 48, True, 1).save(f'{OUTDIR}/{k}_48.png')
    place(k, 24, 24, True, 1).save(f'{OUTDIR}/{k}_24.png')

# ---- bản nét _hd ----
HD, PAD = 240, 5      # 5× ô 48px, cùng bố cục với bản _48 (đáy sát mép, căn giữa, quay trái)
def crop_hd(im):
    a = np.array(im); ys, xs = np.nonzero(a[..., 3] > 8)
    return im.crop((xs.min(), ys.min(), xs.max() + 1, ys.max() + 1))
def hd(sp):
    sp = crop_hd(sp.convert('RGBA')).transpose(Image.FLIP_LEFT_RIGHT)
    s = min((HD - 2 * PAD) / sp.width, (HD - 2 * PAD) / sp.height)
    w, h = round(sp.width * s), round(sp.height * s)
    a = np.array(sp).astype(float); al = a[..., 3:] / 255
    pm = Image.fromarray(np.concatenate([a[..., :3] * al, al * 255], -1).round().astype(np.uint8))
    r = np.array(pm.resize((w, h), Image.LANCZOS)).astype(float)
    al2 = np.clip(r[..., 3:], 0, 255) / 255
    rgb = np.where(al2 > 0.004, r[..., :3] / np.maximum(al2, 1e-3), 0)
    out = np.zeros((HD, HD, 4))
    x0, y0 = (HD - w) // 2, HD - PAD - h
    out[y0:y0+h, x0:x0+w] = np.concatenate([np.clip(rgb, 0, 255), al2 * 255], -1)
    return Image.fromarray(out.round().astype(np.uint8))
for k in BOX:
    hd(RAW[k]).save(f'{OUTDIR}/{k}_hd.png')
