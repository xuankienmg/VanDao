"""Cắt 16 quái (10 thường · 3 tinh anh · 3 Boss) từ bảng thiết kế nền trắng 1024×559.
Tách nền: điểm gần trắng nối với mép khung cắt (giữ chỗ trắng bên trong như mắt, tia sáng), bóc 1–2 lớp viền
còn ám trắng, rồi kẻ viền tối 1px. Xuất giống bộ cũ: <id>.png 320×221 (quay phải), <id>_48.png / _24.png
(quay trái, đáy sát mép) và <id>_hd.png 240×240 (giữ nét ảnh gốc).
Dùng: python tools/cut_monsters2.py tools/monsters2_design.png wwwroot/img"""
import sys
import numpy as np
from PIL import Image
from scipy import ndimage

SRC, OUTDIR = sys.argv[1], sys.argv[2]
a = np.array(Image.open(SRC).convert('RGB')).astype(int)

# (x0, y0, x1, y1) khung cắt · faces: hướng mặt trong ảnh gốc ('L' trái / 'R' phải)
BOX = {
    'quai_yeu_lang':        ((36, 17, 205, 131), 'R'),
    'quai_doc_xa':          ((316, 11, 449, 134), 'R'),
    'quai_thach_tru':       ((558, 18, 725, 132), 'R'),
    'quai_bang_ho':         ((809, 28, 972, 129), 'R'),
    'quai_pha_son_vien':    ((59, 133, 222, 272), 'R'),
    'quai_tinh_thach_tich': ((303, 162, 457, 247), 'R'),
    'quai_huyen_o':         ((568, 156, 700, 262), 'R'),
    'quai_ma_dang':         ((821, 140, 972, 260), 'R'),
    'quai_cot_khuyen':      ((55, 280, 198, 394), 'R'),
    'quai_loi_ung':         ((302, 263, 444, 390), 'R'),
    'tinh_thiet_giap_ho':   ((527, 278, 736, 395), 'R'),
    'tinh_cot_tuong':       ((814, 260, 956, 399), 'R'),
    'boss_tu_ty_ma_vien':   ((279, 390, 491, 559), 'R'),
    'boss_hac_giao':        ((516, 395, 763, 559), 'R'),
    'boss_bang_phuong':     ((763, 387, 1024, 556), 'R'),
    'tinh_hac_mang':        ((0, 406, 256, 559), 'R'),
}

def mask(c):
    near_white = (c.min(-1) > 222) & ((c.max(-1) - c.min(-1)) < 22)
    lab, n = ndimage.label(near_white)
    edge = set(np.unique(np.concatenate([lab[0], lab[-1], lab[:, 0], lab[:, -1]]))) - {0}
    bg = np.isin(lab, list(edge))
    # túi nền trắng kẹt bên trong (vòng rắn, giữa chân): mảng gần trắng đủ lớn cũng là nền; chấm nhỏ (mắt, tia sáng) giữ lại
    sizes = ndimage.sum(near_white, lab, range(1, n + 1))
    bg |= np.isin(lab, [i + 1 for i, sz in enumerate(sizes) if sz >= 24])
    fg = ~bg
    v = c.mean(-1)
    for _ in range(2):                                    # bóc viền ám trắng do khử răng cưa
        ring = fg & ~ndimage.binary_erosion(fg)
        rm = ring & (v > 200) & ((c.max(-1) - c.min(-1)) < 30)
        if not rm.any(): break
        fg &= ~rm
    fg = ndimage.binary_opening(fg, iterations=1)
    lab2, n2 = ndimage.label(fg)
    if n2:
        sizes = ndimage.sum(fg, lab2, range(1, n2 + 1))
        fg = np.isin(lab2, [i + 1 for i, s in enumerate(sizes) if s >= max(30, sizes.max() * .01)])
    return fg

RAW = {}
for k, ((x0, y0, x1, y1), face) in BOX.items():
    c = a[y0:y1, x0:x1]
    RAW[k] = (Image.fromarray(np.dstack([c, mask(c) * 255]).astype(np.uint8)), face)

OL = np.array([11, 8, 6])
def crop(im, th=0):
    al = np.array(im)[..., 3]; ys, xs = np.nonzero(al > th)
    return im.crop((xs.min(), ys.min(), xs.max() + 1, ys.max() + 1))
def resize_pm(im, w, h):
    arr = np.array(im).astype(float); al = arr[..., 3:] / 255
    pm = np.concatenate([arr[..., :3] * al, al * 255], -1)
    r = np.array(Image.fromarray(pm.astype(np.uint8)).resize((w, h), Image.BOX)).astype(float)
    al2 = r[..., 3:] / 255
    rgb = np.where(al2 > 0, r[..., :3] / np.maximum(al2, 1e-6), 0)
    return np.clip(rgb, 0, 255), al2[..., 0]
def outline(rgb, m):
    ring = ndimage.binary_dilation(m) & ~m
    rgb = rgb.copy(); rgb[ring] = OL
    return rgb, m | ring
def oriented(k, face_left):
    sp, face = RAW[k]
    return sp.transpose(Image.FLIP_LEFT_RIGHT) if (face == 'L') != face_left else sp
def place(k, W, H, face_left, pad):
    sp = crop(oriented(k, face_left))
    s = min((W - 2 * pad) / sp.width, (H - 2 * pad) / sp.height)
    w, h = max(1, round(sp.width * s)), max(1, round(sp.height * s))
    rgb, al = resize_pm(sp, w, h)
    m = al >= .5
    canvas = np.zeros((H, W, 3)); cm = np.zeros((H, W), bool)
    x0 = (W - w) // 2; y0 = H - pad - h
    canvas[y0:y0 + h, x0:x0 + w][m] = rgb[m]; cm[y0:y0 + h, x0:x0 + w] = m
    canvas, cm = outline(canvas, cm)
    return Image.fromarray(np.dstack([canvas, cm * 255]).astype(np.uint8))

HD, PAD = 240, 5
def hd(k):
    sp = crop(oriented(k, True).convert('RGBA'), 8)
    s = min((HD - 2 * PAD) / sp.width, (HD - 2 * PAD) / sp.height)
    w, h = round(sp.width * s), round(sp.height * s)
    arr = np.array(sp).astype(float); al = arr[..., 3:] / 255
    pm = Image.fromarray(np.concatenate([arr[..., :3] * al, al * 255], -1).round().astype(np.uint8))
    r = np.array(pm.resize((w, h), Image.LANCZOS)).astype(float)
    al2 = np.clip(r[..., 3:], 0, 255) / 255
    rgb = np.where(al2 > 0.004, r[..., :3] / np.maximum(al2, 1e-3), 0)
    out = np.zeros((HD, HD, 4))
    x0, y0 = (HD - w) // 2, HD - PAD - h
    out[y0:y0 + h, x0:x0 + w] = np.concatenate([np.clip(rgb, 0, 255), al2 * 255], -1)
    return Image.fromarray(out.round().astype(np.uint8))

for k in BOX:
    place(k, 320, 221, False, 2).save(f'{OUTDIR}/{k}.png')
    place(k, 48, 48, True, 1).save(f'{OUTDIR}/{k}_48.png')
    place(k, 24, 24, True, 1).save(f'{OUTDIR}/{k}_24.png')
    hd(k).save(f'{OUTDIR}/{k}_hd.png')
print('ok', len(BOX))
