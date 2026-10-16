#!/usr/bin/env python3
"""Cắt 15 quái mới (bản đồ 10 chương) từ 2 bảng thiết kế nền ô cờ giả 1024×559:
  tools/monsters3_design.png — lưới 5×2: 4 quái thường (#1 #6 #7 #8), 4 tinh anh (#2–#5), 2 Boss dự phòng (#9 #10)
  tools/bosses3_design.png   — 5 Boss Tứ Hung: Thao Thiết, Hỗn Độn, Cùng Kỳ, Cùng Kỳ Vương, Đào Ngột
Tách nền: loang từ mép khung qua điểm xám trung tính (màu ô cờ), túi ô cờ kẹt bên trong nhận ra theo 2 tông ô cờ;
giữ mảng liền lớn (kèm mảnh phụ như đá bay, kiếm vòng). Xuất giống bộ cũ: <id>.png 320×221, _48/_24, _hd 240×240.
python3 tools/cut_monsters3.py tools wwwroot/img"""
import sys
import numpy as np
from PIL import Image
from scipy import ndimage
SRCDIR, OUTDIR = sys.argv[1], sys.argv[2]
IMG = {f: np.array(Image.open(f'{SRCDIR}/{f}').convert('RGB')).astype(int) for f in ('monsters3_design.png', 'bosses3_design.png')}
TONES = {'monsters3_design.png': (252, 188), 'bosses3_design.png': (192, 161)}
CX = [0, 205, 410, 615, 820, 1024]; CY = [0, 280, 559]
def cell(i):                                   # ô thứ i (1..10) của lưới 5×2, chừa đường kẻ
    r, c = divmod(i - 1, 5)
    return (CX[c] + 3, CY[r] + 3, CX[c + 1] - 3, CY[r + 1] - 3)
M, B = 'monsters3_design.png', 'bosses3_design.png'
BOX = {   # id: (ảnh, khung, hướng mặt trong ảnh gốc)
    'quai_reu_thach_nhan':     (M, cell(1), 'R'),
    'quai_doc_chu_nu':         (M, cell(6), 'R', 'glow'),
    'quai_cuu_vi_ho':          (M, cell(7), 'R'),
    'quai_thiet_giap_binh':    (M, cell(8), 'R'),
    'tinh_huyet_y_kiem_khach': (M, cell(2), 'R'),
    'tinh_cot_long':           (M, cell(3), 'L', 'glow'),
    'tinh_loi_ky_lan':         (M, cell(4), 'L'),
    'tinh_kiem_hon':           (M, cell(5), 'R'),
    'boss_am_ma':              (M, cell(9), 'R'),
    'boss_kiem_de':            (M, cell(10), 'R'),
    'boss_thao_thiet':         (B, (2, 25, 335, 262), 'R'),
    'boss_hon_don':            (B, (365, 8, 655, 292), 'R'),
    'boss_cung_ky':            (B, (668, 8, 1012, 278), 'R'),
    'boss_cung_ky_vuong':      (B, (5, 292, 475, 505), 'R'),
    'boss_dao_ngot':           (B, (555, 282, 1024, 548), 'R'),
}
def mask(c, tones, glow=False):
    mx, mn = c.max(-1), c.min(-1); v = c.mean(-1)
    lo, hi = min(tones) - 22, max(tones) + 6
    bgl = ((mx - mn) <= 14) & (v >= lo) & (v <= hi)
    lab, n = ndimage.label(bgl)
    edge = set(np.unique(np.concatenate([lab[0], lab[-1], lab[:, 0], lab[:, -1]]))) - {0}
    bg = np.isin(lab, list(edge))
    # túi ô cờ kẹt bên trong: mảng xám trung tính có đủ tông sáng và tông tối của ô cờ (ảnh gốc nhiễu nên không đòi phẳng),
    # hoặc mảnh nhỏ toàn tông sáng
    T1, T2 = max(tones), min(tones)
    for i in range(1, n + 1):
        if i in edge: continue
        sel = lab == i; cnt = sel.sum()
        if cnt < 25: continue
        vv = v[sel]; fa = (vv >= T1 - 14).mean(); fb = ((vv >= T2 - 8) & (vv <= T2 + 26)).mean()
        if (fa > .3 and fb > .1 and fa + fb > .7) or (cnt <= 120 and fa > .85): bg |= sel
    # túi ô cờ kẹt bên trong (kể cả ô cờ bị quầng sáng màu phủ lên): mảng ít sắc độ, độ sáng chia rõ 2 tông xen kẽ
    soft = ((mx - mn) <= 48) & (v >= lo - 35)
    lab3, n3 = ndimage.label(soft & ~bg)
    for i in range(1, n3 + 1):
        sel = lab3 == i; cnt = sel.sum()
        if cnt < 60: continue
        ys, xs = np.nonzero(sel)
        if ys.min() == 0 or xs.min() == 0 or ys.max() == c.shape[0] - 1 or xs.max() == c.shape[1] - 1: pass
        vv = v[sel]; h, e = np.histogram(vv, bins=32, range=(0, 256))
        p1 = h.argmax(); h2 = h.copy(); h2[max(0, p1 - 3):p1 + 4] = 0; p2 = h2.argmax()
        m1, m2 = (e[p1] + 4), (e[p2] + 4)
        near = (np.abs(vv - m1) < 12) | (np.abs(vv - m2) < 12)
        f1, f2 = (np.abs(vv - m1) < 12).mean(), (np.abs(vv - m2) < 12).mean()
        if 18 <= abs(m1 - m2) <= 90 and near.mean() > .8 and min(f1, f2) > .15: bg |= sel
    if glow:                                   # quầng sáng màu mờ phủ lên ô cờ: loang tiếp qua điểm nhạt ít sắc độ nối với nền
        tint = (c.min(-1) >= 168) & ((mx - mn) <= 42)
        lab4, _ = ndimage.label(tint | bg)
        bg |= np.isin(lab4, np.unique(lab4[bg])) & tint
    fg = ~bg
    for _ in range(2):                         # bóc viền ám màu ô cờ
        ring = fg & ~ndimage.binary_erosion(fg)
        rm = ring & ((mx - mn) <= 18) & (v >= lo - 10)
        if not rm.any(): break
        fg &= ~rm
    fg = ndimage.binary_opening(fg, iterations=1)
    lab2, n2 = ndimage.label(fg)
    if n2:
        sizes = ndimage.sum(fg, lab2, range(1, n2 + 1))
        fg = np.isin(lab2, [i + 1 for i, s in enumerate(sizes) if s >= max(30, sizes.max() * .012)])
    return fg
RAW = {}
for k, (f, (x0, y0, x1, y1), face, *opt) in BOX.items():
    c = IMG[f][y0:y1, x0:x1]
    RAW[k] = (Image.fromarray(np.dstack([c, mask(c, TONES[f], 'glow' in opt) * 255]).astype(np.uint8)), face)
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
