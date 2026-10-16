#!/usr/bin/env python3
"""Đổi màu quái dùng lại ở chương sau (bản đồ 10 chương): sinh <id>_v1.._v4 cho cả 4 cỡ ảnh (gốc, _48, _24, _hd).
  v1 Huyết (đỏ thẫm) · v2 U Minh (tím) · v3 Kim (vàng kim) · v4 Ám (đen ánh lam lạnh)
Cách tô: giữ độ sáng từng điểm (chi tiết, đường viền đen), thay sắc độ bằng màu của bản; điểm rất sáng (mắt, ngọc, lửa)
được tô màu "phát sáng" riêng để không bị bệt.
python3 tools/recolor_monsters.py wwwroot/img            (mọi quái thường + tinh anh có trong IDS)
python3 tools/recolor_monsters.py wwwroot/img quai_doc_xa  (chỉ một loài)"""
import os, sys
import numpy as np
from PIL import Image

IDS = ["quai_yeu_lang", "quai_doc_xa", "quai_thach_tru", "quai_bang_ho", "quai_pha_son_vien",
       "quai_tinh_thach_tich", "quai_huyen_o", "quai_ma_dang", "quai_cot_khuyen", "quai_loi_ung",
       "quai_reu_thach_nhan", "quai_doc_chu_nu", "quai_cuu_vi_ho", "quai_thiet_giap_binh",
       "tinh_thiet_giap_ho", "tinh_cot_tuong", "tinh_hac_mang",
       "tinh_huyet_y_kiem_khach", "tinh_cot_long", "tinh_loi_ky_lan", "tinh_kiem_hon"]
SIZES = ["", "_48", "_24", "_hd"]
# (sắc độ chính °, độ bão hòa, hệ số sáng, sắc độ điểm phát sáng °, độ bão hòa điểm sáng)
VARIANTS = {
    1: (354, 0.80, 0.84, 14, 0.85),    # Huyết
    2: (276, 0.62, 0.86, 300, 0.55),   # U Minh
    3: (43, 0.70, 1.06, 50, 0.45),     # Kim
    4: (218, 0.28, 0.72, 195, 0.70),   # Ám
}


def hsv_to_rgb(h, s, v):
    """h 0..1, s, v 0..1 (mảng) → rgb 0..1."""
    i = np.floor(h * 6).astype(int) % 6
    f = h * 6 - np.floor(h * 6)
    p, q, t = v * (1 - s), v * (1 - f * s), v * (1 - (1 - f) * s)
    r = np.choose(i, [v, q, p, p, t, v]); g = np.choose(i, [t, v, v, q, p, p]); b = np.choose(i, [p, p, t, v, v, q])
    return np.stack([r, g, b], -1)


def recolor(img, var):
    hue, sat, gain, ghue, gsat = VARIANTS[var]
    a = np.asarray(img.convert("RGBA")).astype(float) / 255
    rgb, al = a[..., :3], a[..., 3:]
    mx, mn = rgb.max(-1), rgb.min(-1)
    # độ sáng cảm nhận, kéo giãn nhẹ để thân quái không bị xỉn sau khi đổi sắc
    lum = rgb @ np.array([0.299, 0.587, 0.114])
    v = np.clip(lum * 1.18 * gain, 0, 1)
    s0 = np.where(mx > 0, (mx - mn) / np.maximum(mx, 1e-6), 0)
    # điểm tối (viền) ít sắc; vùng sáng vừa đủ sắc; giữ một phần độ bão hòa gốc để vảy / lông còn chuyển màu
    s = np.clip(sat * (0.55 + 0.45 * s0) * np.clip(v * 3, 0, 1), 0, 1)
    # biến thiên sắc độ nhỏ theo độ sáng: chỗ tối ngả lạnh/thẫm hơn, chỗ sáng ấm hơn → không bệt một màu
    h = ((hue + (v - 0.5) * 16) % 360) / 360
    out = hsv_to_rgb(h, s, v)
    glow = (mx > 0.82) & (s0 > 0.35) | (mx > 0.93)                     # mắt, ngọc, lửa, tia sét
    gv = np.clip(mx * 1.02, 0, 1)
    gl = hsv_to_rgb(np.full_like(gv, ghue / 360), np.clip(gsat * (0.4 + 0.6 * s0), 0, 1), gv)
    out = np.where(glow[..., None], gl, out)
    return Image.fromarray((np.concatenate([out, al], -1) * 255 + 0.5).astype(np.uint8), "RGBA")


def main():
    d = sys.argv[1]
    ids = sys.argv[2:] or IDS
    n = 0
    for mid in ids:
        for sz in SIZES:
            src = os.path.join(d, f"{mid}{sz}.png")
            if not os.path.exists(src):
                continue
            im = Image.open(src)
            for var in VARIANTS:
                recolor(im, var).save(os.path.join(d, f"{mid}_v{var}{sz}.png"), optimize=True)
                n += 1
    print("đã sinh", n, "ảnh")


if __name__ == "__main__":
    main()
