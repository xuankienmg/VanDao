#!/usr/bin/env python3
"""Bỏ hình tu sĩ cũ vẽ sẵn trong khung chiêu Hỏa hệ (img/fx_hoa_skills_a.png) → img/fx_hoa_skills_fx.png chỉ còn hiệu ứng lửa,
để trận đấu vẽ nhân vật theo cảnh giới (hero_cast) bên dưới. Giữ điểm màu lửa (đỏ/cam/vàng, lõi trắng), bỏ màu áo/tóc/da của nhân vật cũ.
python3 tools/clean_skill_hero.py wwwroot/img"""
import sys
import numpy as np
from PIL import Image
d = sys.argv[1]
FW, FH = 124, 64
FEET = [[44,31,28,20,None,45,46,None], [46,25,25,None,None,33,40,None], [48,None,None,None,None,None,None,None], [49,22,25,None,None,36,39,None],
        [49,36,34,None,None,None,None,None], [48,None,None,None,None,None,None,None], [48,65,73,62,63,66,61,None], [50,62,71,66,62,63,63,None]]
im = np.array(Image.open(f'{d}/fx_hoa_skills_a.png').convert('RGBA')).astype(int)
r, g, b, a = im[..., 0], im[..., 1], im[..., 2], im[..., 3]
lum = r * .299 + g * .587 + b * .114
# giữ lửa: đỏ/cam/vàng (xanh lam thấp so với đỏ) hoặc lõi trắng rất sáng; bỏ màu áo lam tím, tóc đen, da, bóng tối của nhân vật cũ
keep = ((r >= 110) & (r >= g) & (b < .45 * r)) | (lum > 235)
# lấp lỗ lốm đốm trong lửa: điểm bị bỏ mà ≥6/8 điểm quanh còn giữ
k = keep.astype(int); n = np.zeros_like(k)
for dy in (-1, 0, 1):
    for dx in (-1, 0, 1):
        if dy or dx: n += np.roll(np.roll(k, dy, 0), dx, 1)
keep |= (n >= 6) & (a > 0)
EXTRA = {(3, 4): 25, (4, 3): 50, (5, 1): 34, (5, 2): 34}
# vùng quanh chân nhân vật (khung có nhân vật): lọc chặt hơn để không còn mảng áo bị lửa hắt sáng
strict = ((r >= 170) & (b < .35 * r) & (g >= .25 * r)) | ((lum > 245) & (b < .9 * r))
for row in range(8):
    for f in range(8):
        x = FEET[row][f]
        x = EXTRA.get((row, f), x)                                               # khung vẽ nhân vật dù feet = null (bảng feet bỏ sót)
        if x is None: continue
        sub = (slice(row * FH, row * FH + FH), slice(f * FW + max(0, x - 17), f * FW + min(FW, x + 17)))
        keep[sub] &= strict[sub]
out = im.copy()
out[..., 3] = np.where(keep, a, 0)
Image.fromarray(out.astype(np.uint8), 'RGBA').save(f'{d}/fx_hoa_skills_fx.png')
print('ok')
