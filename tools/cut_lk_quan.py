#!/usr/bin/env python3
"""Cắt quần Giới Khố (lk_quan) từ ảnh thiết kế nền ô cờ giả → wwwroot/img/lk_quan.png 128×128 (+ _epic: viền hào quang tím Địa cấp).
python3 tools/cut_lk_quan.py tools/lk_quan_design.png wwwroot/img"""
import sys
from collections import deque
import numpy as np
from PIL import Image, ImageFilter
src, out = sys.argv[1], sys.argv[2]
im = np.array(Image.open(src).convert('RGB')).astype(int)
H, W, _ = im.shape
mx, mn = im.max(-1), im.min(-1)
bgl = (mx - mn < 12) & (mn > 105)                 # ô cờ xám sáng trung tính (quần xám tối nên không lẫn)
bg = np.zeros((H, W), bool); seen = np.zeros((H, W), bool)
for y0, x0 in zip(*np.nonzero(bgl)):              # mọi mảng ô cờ lớn (kể cả kẹp giữa hai ống quần) là nền
    if seen[y0, x0]: continue
    seen[y0, x0] = True; dq = deque([(y0, x0)]); pts = []
    while dq:
        y, x = dq.popleft(); pts.append((y, x))
        for dy, dx in ((1, 0), (-1, 0), (0, 1), (0, -1)):
            ny, nx = y + dy, x + dx
            if 0 <= ny < H and 0 <= nx < W and bgl[ny, nx] and not seen[ny, nx]: seen[ny, nx] = True; dq.append((ny, nx))
    if len(pts) > 40:
        for y, x in pts: bg[y, x] = True
soft = (mx - mn < 20) & (mn > 90)
for _ in range(2):
    nb = np.zeros_like(bg); nb[1:] |= bg[:-1]; nb[:-1] |= bg[1:]; nb[:, 1:] |= bg[:, :-1]; nb[:, :-1] |= bg[:, 1:]
    bg |= nb & soft
a = np.where(bg, 0, 255).astype(np.uint8)
ys, xs = np.nonzero(a); y0, y1, x0, x1 = ys.min(), ys.max() + 1, xs.min(), xs.max() + 1
rgba = np.dstack([im.astype(np.uint8), a])[y0:y1, x0:x1]
h, w = rgba.shape[:2]; S = int(max(h, w) * 1.12)          # chừa lề cho hào quang bản Địa cấp
sq = np.zeros((S, S, 4), np.uint8); sq[(S - h) // 2:(S - h) // 2 + h, (S - w) // 2:(S - w) // 2 + w] = rgba
# thu nhỏ trên premultiplied
f = sq.astype(float); al = f[..., 3:] / 255
pm = np.dstack([f[..., :3] * al, f[..., 3:]]).astype(np.uint8)
s = np.array(Image.fromarray(pm, 'RGBA').resize((128, 128), Image.LANCZOS)).astype(float)
al2 = s[..., 3:] / 255
rgb = np.where(al2 > 0, s[..., :3] / np.maximum(al2, 1e-6), 0)
res = np.dstack([np.clip(rgb, 0, 255), s[..., 3:]]).astype(np.uint8)
res[..., 3] = np.where(res[..., 3] < 60, 0, 255)
base = Image.fromarray(res, 'RGBA'); base.save(f'{out}/lk_quan.png')
# Địa cấp: viền tím sáng + quầng mờ + vài điểm lấp lánh
A = Image.fromarray(res[..., 3])
ring1 = np.array(A.filter(ImageFilter.MaxFilter(5))) > 0
ring2 = np.array(A.filter(ImageFilter.MaxFilter(11))) > 0
glow = np.zeros((128, 128, 4), np.uint8)
glow[ring2] = (120, 70, 190, 90); glow[ring1] = (176, 124, 240, 230)
e = Image.fromarray(glow, 'RGBA'); e.alpha_composite(base)
ea = np.array(e)
for (x, y, r) in ((100, 22, 3), (24, 96, 2), (106, 88, 2), (30, 30, 1)):
    for d in range(-r, r + 1):
        for (px, py) in ((x + d, y), (x, y + d)):
            if 0 <= px < 128 and 0 <= py < 128: ea[py, px] = (255, 250, 255, 255)
Image.fromarray(ea, 'RGBA').save(f'{out}/lk_quan_epic.png')
print('ok', (x0, y0, x1, y1))
