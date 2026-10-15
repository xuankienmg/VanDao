#!/usr/bin/env python3
"""Cắt lò đan (tab Luyện chế · Đan dược) từ ảnh thiết kế nền ô cờ giả → 3 lớp cùng khung, nét gấp đôi canvas logic:
  furnace2_body.png  thân lò (kèm tai rồng, chân, lửa xanh trước cửa lò)
  furnace2_lid.png   nắp lò (nâng lên khi thành đan)
  furnace2_runes.png chữ phù xanh trên đai lò (nhấp nháy khi luyện)
python3 tools/cut_furnace.py tools/furnace_design.png wwwroot/img"""
import sys
from collections import deque
import numpy as np
from PIL import Image

src, out = sys.argv[1], sys.argv[2]
OUT_W = 124                                    # px ảnh ra (= 62 px logic × 2)
im = np.array(Image.open(src).convert('RGB')).astype(int)
H, W, _ = im.shape
mx, mn = im.max(-1), im.min(-1)
r, g, b = im[..., 0], im[..., 1], im[..., 2]

# 1) nền ô cờ: xám trung tính, loang từ mép ảnh
bgl = (mx - mn < 14) & (mn > 80) & (mx < 185)
bg = np.zeros((H, W), bool); q = deque()
for y in range(H):
    for x in (0, W - 1):
        if bgl[y, x]: bg[y, x] = 1; q.append((y, x))
for x in range(W):
    for y in (0, H - 1):
        if bgl[y, x] and not bg[y, x]: bg[y, x] = 1; q.append((y, x))
while q:
    y, x = q.popleft()
    for dy, dx in ((1, 0), (-1, 0), (0, 1), (0, -1)):
        ny, nx = y + dy, x + dx
        if 0 <= ny < H and 0 <= nx < W and not bg[ny, nx] and bgl[ny, nx]: bg[ny, nx] = 1; q.append((ny, nx))
# viền pha (ô cờ lẫn mép nét) còn sót sát nền: vài vòng nới
soft = (mx - mn < 22) & (mn > 70) & (mx < 200)
for _ in range(3):
    nb = np.zeros_like(bg); nb[1:] |= bg[:-1]; nb[:-1] |= bg[1:]; nb[:, 1:] |= bg[:, :-1]; nb[:, :-1] |= bg[:, 1:]
    bg |= nb & soft
keep = ~bg
# ô cờ bị nét bao kín (trong vòng tai rồng): mảng xám trung tính lớn ở bất kỳ đâu cũng là nền
lab0 = np.zeros((H, W), int); c0 = 0
for yy, xx in zip(*np.nonzero(bgl & keep)):
    if lab0[yy, xx]: continue
    c0 += 1; lab0[yy, xx] = c0; dq = deque([(yy, xx)]); pts = []
    while dq:
        y, x = dq.popleft(); pts.append((y, x))
        for dy, dx in ((1, 0), (-1, 0), (0, 1), (0, -1)):
            ny, nx = y + dy, x + dx
            if 0 <= ny < H and 0 <= nx < W and bgl[ny, nx] and keep[ny, nx] and not lab0[ny, nx]: lab0[ny, nx] = c0; dq.append((ny, nx))
    if len(pts) > 150:
        for y, x in pts: bg[y, x] = True
for _ in range(3):
    nb = np.zeros_like(bg); nb[1:] |= bg[:-1]; nb[:-1] |= bg[1:]; nb[:, 1:] |= bg[:, :-1]; nb[:, :-1] |= bg[:, 1:]
    bg |= nb & soft
keep = ~bg
# 2) khói trắng trên miệng lò (canvas tự vẽ khói động) và mọi thứ phía trên núm nắp
keep[:198] = False
smoke = (mx > 160) & (mx - mn < 30)
keep[198:235][smoke[198:235]] = False
# 3) bỏ đốm sáng / dấu ✦ rời rạc: chỉ giữ mảng liền lớn nhất (thân lò)
lab = np.zeros((H, W), int); sizes = {}; cur = 0
for y0, x0 in zip(*np.nonzero(keep)):
    if lab[y0, x0]: continue
    cur += 1; lab[y0, x0] = cur; dq = deque([(y0, x0)]); n = 0
    while dq:
        y, x = dq.popleft(); n += 1
        for dy, dx in ((1, 0), (-1, 0), (0, 1), (0, -1)):
            ny, nx = y + dy, x + dx
            if 0 <= ny < H and 0 <= nx < W and keep[ny, nx] and not lab[ny, nx]: lab[ny, nx] = cur; dq.append((ny, nx))
    sizes[cur] = n
big = max(sizes, key=sizes.get)
keep = lab == big
ys, xs = np.nonzero(keep); y0, y1, x0, x1 = ys.min(), ys.max() + 1, xs.min(), xs.max() + 1
print('khung lò', x0, y0, x1, y1)

# 4) tách nắp: các hàng trên đai chữ, giữa hai tai rồng
LID_BOTTOM, LID_X0, LID_X1 = 402, 262, 768
lid = keep.copy(); lid[LID_BOTTOM:] = False; lid[:, :LID_X0] = False; lid[:, LID_X1:] = False
body = keep & ~lid
# 5) chữ phù: điểm xanh lục trên đai (hàng 405–470)
rune = keep & (g > r + 12) & (g > b + 8) & (g > 75)
rune[:405] = False; rune[470:] = False

def layer(mask, boost=False):
    a = np.where(mask, 255, 0).astype(np.uint8)
    rgb = im.copy()
    if boost: rgb = np.clip(rgb * 1.25 + 20, 0, 255)
    arr = np.dstack([rgb.astype(np.uint8), a])[y0:y1, x0:x1]
    h = round(OUT_W * (y1 - y0) / (x1 - x0))
    # thu nhỏ trên premultiplied để mép không ám màu nền
    f = arr.astype(float); al = f[..., 3:] / 255
    pm = np.dstack([f[..., :3] * al, f[..., 3:]]).astype(np.uint8)
    s = np.array(Image.fromarray(pm, 'RGBA').resize((OUT_W, h), Image.LANCZOS)).astype(float)
    al2 = s[..., 3:] / 255
    rgb2 = np.where(al2 > 0, s[..., :3] / np.maximum(al2, 1e-6), 0)
    res = np.dstack([np.clip(rgb2, 0, 255), s[..., 3:]]).astype(np.uint8)
    res[..., 3] = np.where(res[..., 3] < 50, 0, np.where(res[..., 3] > 190, 255, res[..., 3]))
    return Image.fromarray(res, 'RGBA')

layer(body).save(f'{out}/furnace2_body.png')
layer(lid).save(f'{out}/furnace2_lid.png')
# chữ phù: lấy sau khi thu nhỏ (nét chữ mảnh, lọc trước khi thu nhỏ thì mất), điểm xanh trên đai, làm sáng lên
bl = np.array(layer(body)).astype(int)
sy0 = round((405 - y0) * OUT_W / (x1 - x0)); sy1 = round((470 - y0) * OUT_W / (x1 - x0))
R_, G_, B_, A_ = bl[..., 0], bl[..., 1], bl[..., 2], bl[..., 3]
m = (G_ > R_ + 10) & (G_ > B_ + 4) & (G_ > 70) & (A_ > 0)
m[:sy0] = False; m[sy1:] = False
ru = np.zeros_like(bl); ru[m] = np.dstack([np.clip(R_ * 1.3 + 30, 0, 255), np.clip(G_ * 1.3 + 40, 0, 255), np.clip(B_ * 1.3 + 30, 0, 255), A_])[m]
Image.fromarray(ru.astype(np.uint8), 'RGBA').save(f'{out}/furnace2_runes.png')
print('ok', OUT_W, round(OUT_W * (y1 - y0) / (x1 - x0)))
