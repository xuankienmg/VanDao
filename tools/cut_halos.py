#!/usr/bin/env python3
"""Cắt vòng sáng sau lưng nhân vật (menu Đột phá) từ ảnh thiết kế 5 × 2 ô trên nền ô cờ tối.
Lấy 9 vòng đầu (vòng 10 bỏ), mỗi vòng 1 ảnh vuông OUT×OUT, tâm vòng ở giữa ảnh để xoay không lệch.
Nền ô cờ tối (2 tông ≈ 10 và ≈ 35): độ trong suốt = phần sáng vượt trên nền, màu tách khỏi nền đen
→ quầng sáng mờ giữ lại dạng bán trong suốt, ô cờ biến mất.
python3 tools/cut_halos.py tools/halos_design.png wwwroot/img"""
import sys
import numpy as np
from PIL import Image
from scipy import ndimage

SRC, OUT_DIR = sys.argv[1], sys.argv[2]
OUT, COUNT = 320, 9
BG = 42                      # ngưỡng tông sáng nhất của ô cờ (dưới mức này coi là nền)
CX = [0, 205, 410, 615, 820, 1024]
ROWS = [(0, 240), (286, 526)]   # bỏ dòng chữ tên vòng dưới mỗi ô
PAD = 12                     # chừa quầng sáng ngoài vòng sáng chính

im = np.asarray(Image.open(SRC).convert("RGB")).astype(float)
mx = im.max(-1)
alpha = np.clip((mx - BG) / (255 - BG), 0, 1)
# màu thật của lớp sáng: bỏ phần nền tối trộn vào (nền ≈ BG ở mọi kênh)
rgb = np.clip((im - BG * (1 - alpha[..., None])) / np.maximum(alpha[..., None], 1e-3), 0, 255)
n = 0
for (y0, y1) in ROWS:
    for c in range(5):
        if n >= COUNT:
            break
        x0, x1 = CX[c], CX[c + 1]
        cell = alpha[y0:y1, x0:x1]
        m = cell > 0.15
        lab, k = ndimage.label(m)
        sizes = ndimage.sum(m, lab, range(1, k + 1))
        keep = np.isin(lab, [i + 1 for i, s in enumerate(sizes) if s > 30])
        ys, xs = np.nonzero(keep)
        cy, cx = (ys.min() + ys.max()) / 2 + y0, (xs.min() + xs.max()) / 2 + x0
        R = max(ys.max() - ys.min(), xs.max() - xs.min()) / 2 + PAD
        # chỉ giữ trong ô và trong hình tròn bán kính R quanh tâm (bỏ hạt sáng của ô bên cạnh)
        yy, xx = np.mgrid[0:im.shape[0], 0:im.shape[1]]
        inside = ((yy - cy) ** 2 + (xx - cx) ** 2 <= R ** 2) & (xx >= x0) & (xx < x1) & (yy >= y0) & (yy < y1)
        a = np.where(inside, alpha, 0)
        box = (int(round(cx - R)), int(round(cy - R)), int(round(cx + R)), int(round(cy + R)))
        rgba = np.dstack([rgb * a[..., None], a * 255]).astype(np.uint8)   # premultiplied để thu phóng không viền đen
        tile = Image.fromarray(rgba, "RGBA").crop(box).resize((OUT, OUT), Image.LANCZOS)
        t = np.asarray(tile).astype(float)
        al = t[..., 3:] / 255
        col = np.where(al > 0, t[..., :3] / np.maximum(al, 1e-3), 0)
        res = np.dstack([np.clip(col, 0, 255), t[..., 3:]]).astype(np.uint8)
        res[..., 3] = np.where(res[..., 3] < 4, 0, res[..., 3])
        n += 1
        Image.fromarray(res, "RGBA").save(f"{OUT_DIR}/halo_{n:02d}.png", optimize=True)
        print(f"halo_{n:02d}", "tâm", round(cx), round(cy), "R", round(R))
