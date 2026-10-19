"""Skin Luyện Hư (tóc bạc, đạo bào lam thêu rồng) từ tools/hero_luyenhu_design.png (nền trong suốt, 4 tư thế:
ngồi thiền · đứng · thế chiến đấu · tung chiêu). Bỏ quầng sáng mờ (alpha thấp) quanh người, giữ hiệu ứng dính người.
Xuất vào img: hero_luyenhu_stand.png (360px) · hero_luyenhu.png / _48 / _24 (sprite quay phải) · med_luyenhu.png (400px)
· hoa_cau_ref_luyenhu.png (bảng 8 khung trận đấu, cùng bố cục hoa_cau_ref.png) · skin/luyenhu_combat.png, skin/luyenhu_cast.png (để dành).
Dùng: python tools/cut_luyenhu.py tools wwwroot/img"""
import sys, os
import numpy as np
from PIL import Image
from scipy import ndimage
SRC, OUT = sys.argv[1], sys.argv[2]

def crop(im):
    al = np.array(im)[..., 3]; ys, xs = np.nonzero(al)
    return im.crop((xs.min(), ys.min(), xs.max() + 1, ys.max() + 1))
def fit_h(im, h):
    w = round(im.width * h / im.height)
    arr = np.array(im).astype(float); al = arr[..., 3:] / 255
    pm = Image.fromarray(np.concatenate([arr[..., :3] * al, al * 255], -1).round().astype(np.uint8))
    r = np.array(pm.resize((w, h), Image.LANCZOS)).astype(float)
    a2 = np.clip(r[..., 3:], 0, 255) / 255
    rgb = np.where(a2 > .004, r[..., :3] / np.maximum(a2, 1e-3), 0)
    return Image.fromarray(np.concatenate([np.clip(rgb, 0, 255), a2 * 255], -1).round().astype(np.uint8))
OL = np.array([11, 8, 6])
def sprite(im, S, pad=1):
    sp = crop(im)
    s = min((S - 2 * pad) / sp.width, (S - 2 * pad) / sp.height)
    w, h = max(1, round(sp.width * s)), max(1, round(sp.height * s))
    arr = np.array(sp).astype(float); al = arr[..., 3:] / 255
    pm = Image.fromarray(np.concatenate([arr[..., :3] * al, al * 255], -1).astype(np.uint8)).resize((w, h), Image.BOX)
    r = np.array(pm).astype(float); a2 = r[..., 3] / 255
    rgb = np.where(a2[..., None] > 0, r[..., :3] / np.maximum(a2[..., None], 1e-6), 0)
    m = a2 >= .5
    can = np.zeros((S, S, 3)); cm = np.zeros((S, S), bool)
    x0, y0 = (S - w) // 2, S - pad - h
    can[y0:y0 + h, x0:x0 + w][m] = rgb[m]; cm[y0:y0 + h, x0:x0 + w] = m
    ring = ndimage.binary_dilation(cm) & ~cm; can[ring] = OL; cm |= ring
    return Image.fromarray(np.dstack([can, cm * 255]).astype(np.uint8))

im = np.array(Image.open(f'{SRC}/hero_luyenhu_design.png').convert('RGBA'))
a = im[..., 3]; hi = a > 200
lab, n = ndimage.label(hi); sz = ndimage.sum(hi, lab, range(1, n + 1))
poses = {}
for i in np.argsort(-sz)[:4]:
    sl = ndimage.find_objects(lab)[i]; cx = (sl[1].start + sl[1].stop) / 2
    nm = 'med' if cx < 400 else 'stand' if cx < 800 else 'combat' if cx < 1200 else 'cast'
    m = ndimage.binary_dilation(ndimage.binary_fill_holes(lab == i + 1), iterations=2) & (a > 120)
    out = im.copy(); out[..., 3] = np.where(m, a, 0)
    poses[nm] = crop(Image.fromarray(out))
stand = poses['stand']
fit_h(stand, 360).save(f'{OUT}/hero_luyenhu_stand.png')
fit_h(stand, 360).save(f'{OUT}/hero_luyenhu.png')
sprite(stand, 48).save(f'{OUT}/hero_luyenhu_48.png')
sprite(stand, 24).save(f'{OUT}/hero_luyenhu_24.png')
fit_h(poses['med'], 400).save(f'{OUT}/med_luyenhu.png')
os.makedirs(f'{OUT}/skin', exist_ok=True)
fit_h(poses['combat'], 400).save(f'{OUT}/skin/luyenhu_combat.png')
fit_h(poses['cast'], 400).save(f'{OUT}/skin/luyenhu_cast.png')

# bảng khung trận đấu: như tools/cut_heroes.py (khung 0/7 đứng · 1–3 nhoài tới + cầu lửa · 4–6 giữ bản gốc)
FW, FH = 168, 158
FEET = [(76, 153), (70, 150), (38, 148), (33, 146), None, None, None, (33, 146)]
FIRE = [None, (111, 80), (126, 82), (144, 76), None, None, None, None]
SCALE = [1, 1.107, 1.069, 1.204, 1, 1, 1, 1.204]
LEAN = [0, -3, -5, -7, 0, 0, 0, 2]
HERO_H = 124
base = Image.open(f'{OUT}/hoa_cau_ref.png').convert('RGBA')
def orb(size):
    r = size; yy, xx = np.mgrid[-r:r + 1, -r:r + 1]; d = np.sqrt(xx ** 2 + yy ** 2) / r
    al = np.clip(1 - d, 0, 1) ** .7
    col = np.stack([255 * np.ones_like(d), np.clip(110 + 160 * (1 - d), 0, 255), np.clip(30 + 225 * (1 - d * 1.6), 0, 255)], -1)
    return Image.fromarray(np.dstack([col, al * 255]).astype(np.uint8))
fig = crop(Image.open(f'{OUT}/hero_luyenhu.png').convert('RGBA'))
sheet = Image.new('RGBA', (FW * 8, FH), (0, 0, 0, 0))
for i in range(8):
    if FEET[i] is None:
        sheet.paste(base.crop((i * FW, 0, (i + 1) * FW, FH)), (i * FW, 0)); continue
    h = round(HERO_H / SCALE[i]); f = fit_h(fig, h)
    if LEAN[i]: f = f.rotate(LEAN[i], resample=Image.BICUBIC, expand=True, center=(f.width // 2, f.height))
    f = crop(f); fx, fy = FEET[i]
    sheet.alpha_composite(f, (i * FW + fx - f.width // 2, fy - f.height))
    if FIRE[i]:
        o = orb([8, 12, 16][i - 1]); sheet.alpha_composite(o, (i * FW + FIRE[i][0] - o.width // 2, FIRE[i][1] - o.height // 2))
sheet.save(f'{OUT}/hoa_cau_ref_luyenhu.png')
print('ok')
