#!/usr/bin/env python3
"""Tuyệt kỹ Hỏa hệ (Xích Diễm Thiên, hàng 5 của bảng khung chiêu) đổi lửa đỏ → lửa lam.
Đổi màu điểm "nóng" (đỏ/cam/vàng) theo độ sáng sang dải lam: lam thẫm → lam → xanh băng → trắng; giữ alpha. Chạy lại không đổi thêm.
Chạy sau tools/clean_skill_hero.py (bảng _fx tạo lại từ bản đỏ):
python3 tools/blue_ultimate.py wwwroot/img/fx_hoa_skills_fx.png wwwroot/img/fx_hoa_skills.png"""
import sys
import numpy as np
from PIL import Image
ROW, FH = 5, 64
STOPS = np.array([[0, 6, 18, 70], [70, 20, 70, 190], [140, 40, 140, 245], [200, 110, 215, 255], [240, 210, 245, 255], [256, 250, 255, 255]], float)
def ramp(l):
    out = np.zeros(l.shape + (3,))
    for c in range(3): out[..., c] = np.interp(l, STOPS[:, 0], STOPS[:, c + 1])
    return out
for path in sys.argv[1:]:
    im = np.array(Image.open(path).convert('RGBA')).astype(float)
    sub = im[ROW * FH:(ROW + 1) * FH]
    r, g, b = sub[..., 0], sub[..., 1], sub[..., 2]
    hot = (r > b + 15) & (r >= g * .9) & (r > 40)                    # lửa (đỏ/cam/vàng); áo lam/tóc đen của nhân vật xem trước không đổi
    lum = r * .35 + g * .55 + b * .10
    sub[..., :3] = np.where(hot[..., None], ramp(np.clip(lum * 1.08, 0, 255)), sub[..., :3])
    # khói đỏ sẫm → lam sẫm sẽ thành mảng tối có mép vuông theo khung: làm mờ dần các điểm tối
    sub[..., 3] = np.where(hot, sub[..., 3] * np.clip(lum / 110, 0, 1) ** 1.3, sub[..., 3])
    im[ROW * FH:(ROW + 1) * FH] = sub
    Image.fromarray(np.clip(im, 0, 255).astype(np.uint8), 'RGBA').save(path)
    print('ok', path)
