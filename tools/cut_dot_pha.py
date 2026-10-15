#!/usr/bin/env python3
"""Cắt icon nút Đột phá từ ảnh thiết kế (nền ô cờ giả + nền xanh đậm) → img/ui_dot_pha.png 32×32 (+ _64 bản nét).
python3 tools/cut_dot_pha.py tools/ui_dot_pha_design.png wwwroot/img"""
import sys
from collections import deque
import numpy as np
from PIL import Image
src, out = sys.argv[1], sys.argv[2]
im = np.array(Image.open(src).convert('RGB')).astype(int)
im = im[100:890, 115:915]                      # khung xanh + vành nước, bỏ dấu ✦ góc dưới
H, W, _ = im.shape
r, g, b = im[..., 0], im[..., 1], im[..., 2]
mx, mn = im.max(-1), im.min(-1)
checker = (mx - mn < 14) & (mn > 90) & (mx < 180)
navy = (r < 60) & (g < 72) & (b < 115) & (b > r + 15)
bgl = checker | navy
bg = np.zeros((H, W), bool); q = deque()
for y in range(H):
    for x in (0, W - 1):
        if bgl[y, x] and not bg[y, x]: bg[y, x] = 1; q.append((y, x))
for x in range(W):
    for y in (0, H - 1):
        if bgl[y, x] and not bg[y, x]: bg[y, x] = 1; q.append((y, x))
for y in range(55, H - 60, 4):                 # hạt giống trong nền xanh (mép trong khung), vì viền khung chặn đường từ ô cờ vào
    for x in (55, W - 60):
        if navy[y, x] and not bg[y, x]: bg[y, x] = 1; q.append((y, x))
for x in range(55, W - 60, 4):
    if navy[55, x] and not bg[55, x]: bg[55, x] = 1; q.append((55, x))
while q:
    y, x = q.popleft()
    for dy, dx in ((1, 0), (-1, 0), (0, 1), (0, -1)):
        ny, nx = y + dy, x + dx
        if 0 <= ny < H and 0 <= nx < W and not bg[ny, nx] and bgl[ny, nx]:
            bg[ny, nx] = 1; q.append((ny, nx))
# viền mép khung xanh (pha trộn ô cờ/xanh) còn sót: bỏ điểm tối, ít bão hòa sát nền
dark_edge = (mx < 95) & ~navy
for _ in range(3):
    nb = np.zeros_like(bg); nb[1:] |= bg[:-1]; nb[:-1] |= bg[1:]; nb[:, 1:] |= bg[:, :-1]; nb[:, :-1] |= bg[:, 1:]
    bg |= nb & (dark_edge | ((mx - mn) < 30) & (mx < 190))
a = (~bg).astype(np.uint8) * 255
# bỏ mảnh vụn nhỏ cô lập (<40 px)
from itertools import product
lab = np.zeros((H, W), int); cur = 0; sizes = {}
for y0, x0 in zip(*np.nonzero(a)):
    if lab[y0, x0]: continue
    cur += 1; lab[y0, x0] = cur; dq = deque([(y0, x0)]); n = 0
    while dq:
        y, x = dq.popleft(); n += 1
        for dy, dx in ((1, 0), (-1, 0), (0, 1), (0, -1)):
            ny, nx = y + dy, x + dx
            if 0 <= ny < H and 0 <= nx < W and a[ny, nx] and not lab[ny, nx]: lab[ny, nx] = cur; dq.append((ny, nx))
    sizes[cur] = n
small = np.isin(lab, [k for k, v in sizes.items() if v < 40])
a[small] = 0
rgba = np.dstack([im.astype(np.uint8), a])
ys, xs = np.nonzero(a); y0, y1, x0, x1 = ys.min(), ys.max() + 1, xs.min(), xs.max() + 1
rgba = rgba[y0:y1, x0:x1]
h, w = rgba.shape[:2]; S = max(h, w)
sq = np.zeros((S, S, 4), np.uint8); sq[(S - h) // 2:(S - h) // 2 + h, (S - w) // 2:(S - w) // 2 + w] = rgba
big = Image.fromarray(sq, 'RGBA')
big.save(f'{out}/ui_dot_pha_full.png')
def small_icon(n):
    # thu nhỏ trên nền premultiplied để mép không ám màu
    arr = np.array(big).astype(float); al = arr[..., 3:] / 255
    pm = np.dstack([arr[..., :3] * al, arr[..., 3:]]).astype(np.uint8)
    s = np.array(Image.fromarray(pm, 'RGBA').resize((n, n), Image.LANCZOS)).astype(float)
    al2 = np.clip(s[..., 3:], 0, 255) / 255
    rgb = np.where(al2 > 0, s[..., :3] / np.maximum(al2, 1e-6), 0)
    res = np.dstack([np.clip(rgb, 0, 255), np.clip(s[..., 3:], 0, 255)]).astype(np.uint8)
    res[..., 3] = np.where(res[..., 3] < 40, 0, np.where(res[..., 3] > 200, 255, res[..., 3]))
    return Image.fromarray(res, 'RGBA')
small_icon(32).save(f'{out}/ui_dot_pha.png'); small_icon(64).save(f'{out}/ui_dot_pha_64.png')
print('ok', S)
