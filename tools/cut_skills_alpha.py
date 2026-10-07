"""Tách nền 64 khung animation Hỏa hệ (fx_hoa_skills.png) thành bản trong suốt để chồng vào cảnh trận đánh.
Dùng: python tools/cut_skills_alpha.py <wwwroot/img>  → fx_hoa_skills_a.png + in mốc (chân nhân vật / tâm hiệu ứng) từng khung."""
import sys, json
import numpy as np
from PIL import Image
from scipy import ndimage
d = sys.argv[1]
src = np.array(Image.open(f'{d}/fx_hoa_skills.png').convert('RGB')).astype(float)
FW, FH = 124, 64
def REGION(r, c):                  # cột x có nhân vật trong khung (None = khung chỉ có hiệu ứng)
    if c == 7: return None
    if c == 0: return (8, 70)
    if r in (0, 1, 3) and c in (1, 2, 5, 6): return (4, 58)
    if r in (6, 7): return (36, 90)
    if r == 4 and c in (1, 2): return (8, 56)
    return None
BG = np.median(np.concatenate([src[:2].reshape(-1, 3), src[:, :2].reshape(-1, 3)]), 0)
out = np.zeros((src.shape[0], src.shape[1], 4), np.uint8)
meta = []
for r in range(8):
    row = []
    for c in range(8):
        a = src[r*FH:(r+1)*FH, c*FW:(c+1)*FW]
        dist = np.sqrt(((a - BG) ** 2).sum(-1)); mx = a.max(-1)
        reddark = (a[..., 0] > a[..., 2] + 6) & (mx < 150)
        fire = (mx > 150) & (a[..., 0] >= a[..., 2])
        core = (dist > 22) & ~reddark & ~fire & (a[..., 2] >= a[..., 0] - 10)     # áo/tóc nhân vật (lam tối)
        # vùng có nhân vật (bị lửa hắt đỏ nên không nhận theo màu được): coi mọi điểm khác nền rõ là thân người
        reg = REGION(r, c)
        if reg:
            x0, x1 = reg; zone = np.zeros_like(core); zone[:, x0:x1] = True
            core |= zone & (dist > 30) & (mx < 200)
        lab, n = ndimage.label(core); sz = ndimage.sum(core, lab, range(1, n + 1)) if n else []
        core = np.isin(lab, [i + 1 for i, v in enumerate(sz) if v >= 60])
        core = ndimage.binary_closing(core, iterations=1) | core
        body = ndimage.binary_fill_holes(core | (fire & ndimage.binary_dilation(core, iterations=2)))
        ring = ndimage.binary_dilation(body, iterations=1) & ~body & (mx < 45)
        body |= ring
        alpha = np.clip((dist - 10) / 70, 0, 1) * np.clip((mx - 25) / 80, 0, 1)   # lửa/quầng: mờ theo độ sáng
        alpha[fire] = 1; alpha[body] = 1
        alpha[:, :2] = 0; alpha[:, -2:] = 0
        am = np.maximum(alpha, 1e-3)[..., None]
        col = np.where(body[..., None], a, np.clip((a - BG * (1 - am)) / am, 0, 255))
        out[r*FH:(r+1)*FH, c*FW:(c+1)*FW] = np.dstack([col, alpha * 255]).astype(np.uint8)
        ys, xs = np.nonzero(body)
        if len(xs) > 120:
            yb = ys.max(); sel = ys >= yb - 4
            feet = [int(round(xs[sel].mean())), int(yb)]; h = int(yb - ys.min())
        else: feet, h = None, 0
        ys2, xs2 = np.nonzero(alpha > .5)
        cen = [int(xs2.mean()), int(ys2.mean())] if len(xs2) else [62, 32]
        row.append({'feet': feet, 'h': h, 'c': cen})
    meta.append(row)
Image.fromarray(out, 'RGBA').save(f'{d}/fx_hoa_skills_a.png', optimize=True)
print(json.dumps(meta))
