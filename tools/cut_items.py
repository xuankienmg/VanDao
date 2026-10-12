"""Cắt icon trang bị từ ảnh thiết kế 'Luyện Khí Giới Set': tách nền gradient bằng loang vùng (bước màu nhỏ), xuất 128×128.
Dùng: python tools/cut_items.py <ảnh> <thư mục ra>"""
import sys
from collections import deque
import numpy as np
from PIL import Image
from scipy import ndimage
src, out = sys.argv[1], sys.argv[2]
im = np.array(Image.open(src).convert('RGB')).astype(int)
BOX = {   # id: (x0, y0, x1, y1) trong ảnh 1024×882
    'lk_mu':   (128, 148, 272, 262), 'lk_ao':  (318, 98, 502, 382),
    'lk_nhan': (558, 318, 642, 405), 'lk_kiem': (72, 342, 308, 578), 'lk_gang': (323, 443, 482, 598),
    'lk_giay': (528, 468, 667, 607), 'pb_holo': (738, 448, 837, 567), 'pb_guong': (848, 448, 967, 577),
    'pb_kiem': (788, 558, 977, 746),
}
MASK_OUT = {'pb_kiem': [(858, 700, 977, 746)]}   # chữ "Kiếm" đè lên ảnh
STEP = 9
STEPS = {'lk_ao': 4, 'pb_holo': 4}   # vật có mảng màu mịn gần nền: bước loang nhỏ hơn
for k, (x0, y0, x1, y1) in BOX.items():
    a = im[y0:y1, x0:x1]; H, W, _ = a.shape
    bg = np.zeros((H, W), bool); q = deque()
    for y in range(H):
        for x in (0, W - 1): bg[y, x] = True; q.append((y, x))
    for x in range(W):
        for y in (0, H - 1):
            if not bg[y, x]: bg[y, x] = True; q.append((y, x))
    while q:
        y, x = q.popleft(); c = a[y, x]
        for dy, dx in ((1, 0), (-1, 0), (0, 1), (0, -1)):
            yy, xx = y + dy, x + dx
            if 0 <= yy < H and 0 <= xx < W and not bg[yy, xx] and np.abs(a[yy, xx] - c).max() <= STEPS.get(k, STEP):
                bg[yy, xx] = True; q.append((yy, xx))
    obj = ~bg
    for (mx0, my0, mx1, my1) in MASK_OUT.get(k, []):
        obj[max(0, my0 - y0):my1 - y0, max(0, mx0 - x0):mx1 - x0] = False
    lab, n = ndimage.label(obj); sz = ndimage.sum(obj, lab, range(1, n + 1))
    obj = np.isin(lab, [i + 1 for i, v in enumerate(sz) if v >= .03 * sz.max()])
    obj = ndimage.binary_fill_holes(obj)
    rgba = np.dstack([a, obj * 255]).astype(np.uint8)
    img = Image.fromarray(rgba, 'RGBA'); img = img.crop(img.getbbox())
    s = max(img.size); sq = Image.new('RGBA', (s + 8, s + 8)); sq.paste(img, ((s + 8 - img.width) // 2, (s + 8 - img.height) // 2))
    sq.resize((128, 128), Image.LANCZOS).save(f'{out}/{k}.png', optimize=True)
    print(k, img.size)
