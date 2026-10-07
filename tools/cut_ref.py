"""Cắt nhân vật + hiệu ứng Hỏa Cầu thẳng từ ảnh thiết kế, tách nền tối thành trong suốt.

Xuất hoa_cau_ref.png (khung 160x140, 1 hàng) + hoa_cau_ref.json (toạ độ mốc từng khung).
Thứ tự khung: 0 chuẩn bị · 1 tụ hỏa · 2 hình thành · 3 phóng ra · 4 hỏa cầu bay · 5 nổ · 6 tàn lửa
"""
import json, sys
from collections import deque
import numpy as np
from PIL import Image

SRC = sys.argv[1]; OUT = sys.argv[2] if len(sys.argv) > 2 else '.'
im = np.array(Image.open(SRC).convert('RGB')).astype(float)
BG = np.array([15, 21, 31.])

STEP = 189.7
def card(i):                       # vùng vẽ bên trong thẻ khung thứ i (bỏ số thứ tự và nhãn)
    x0 = int(20 + i * STEP)
    return (x0 + 4, 214, x0 + 172, 372)

def cutout(box):
    x0, y0, x1, y1 = box
    a = im[y0:y1, x0:x1]
    H, W, _ = a.shape
    dist = np.sqrt(((a - BG) ** 2).sum(-1))
    mx = a.max(-1)
    bgish = dist < 9
    dark = mx < 42                                             # nền + bóng tối quanh hình
    glow = (a[..., 0] > a[..., 2] + 6) & (mx < 150)           # quầng lửa tối (đỏ, chưa sáng)
    passable = dark | glow
    flood = np.zeros((H, W), bool); q = deque()
    for y in range(H):
        for x in (0, W - 1):
            if passable[y, x]: flood[y, x] = True; q.append((y, x))
    for x in range(W):
        for y in (0, H - 1):
            if passable[y, x] and not flood[y, x]: flood[y, x] = True; q.append((y, x))
    while q:
        y, x = q.popleft()
        for dy, dx in ((1, 0), (-1, 0), (0, 1), (0, -1)):
            yy, xx = y + dy, x + dx
            if 0 <= yy < H and 0 <= xx < W and not flood[yy, xx] and passable[yy, xx]:
                flood[yy, xx] = True; q.append((yy, xx))
    from scipy import ndimage
    # khoảng nền bị bao kín (giữa chân, dưới tay) cũng bỏ nếu đủ lớn
    lab, n = ndimage.label(bgish & ~flood)
    for k in range(1, n + 1):
        m = lab == k
        if m.sum() >= 25: flood |= m
    kept = ~flood
    ring = ndimage.binary_dilation(kept, iterations=1) & flood & dark   # trả lại viền tối 1px
    alpha = np.where(kept | ring, 1.0, 0.0)
    g = flood & ~ring & glow                                     # quầng lửa: mờ dần theo độ sáng
    alpha[g] = np.clip((mx[g] - 45) / 90, 0, 1)
    alpha[:, :4] = 0; alpha[:, -7:] = 0; alpha[:3] = 0                # viền thẻ khung
    col = a.copy()
    out = np.dstack([col, alpha * 255]).astype(np.uint8)
    return Image.fromarray(out, 'RGBA')

def cutout_char(box):
    """Nhân vật (+ lửa trên tay): lấy khối khác nền rõ rệt, lấp lỗ nhỏ (đai đỏ), quầng lửa mờ dần."""
    from scipy import ndimage
    x0, y0, x1, y1 = box
    a = im[y0:y1, x0:x1]
    dist = np.sqrt(((a - BG) ** 2).sum(-1)); mx = a.max(-1)
    reddark = (a[..., 0] > a[..., 2] + 6) & (mx < 150)
    fire = (mx > 150) & (a[..., 0] >= a[..., 2])                    # lửa sáng, lõi trắng, da
    core = (dist > 16) & ~reddark & ~fire
    lab, n = ndimage.label(core)
    sizes = ndimage.sum(core, lab, range(1, n + 1))
    core = np.isin(lab, [i + 1 for i, s in enumerate(sizes) if s >= 120])
    core |= fire & ndimage.binary_dilation(core, iterations=2)          # da tay/mặt sát thân
    # lấp lỗ nhỏ bên trong (đai lưng đỏ, nếp áo) nhưng chừa khe lớn giữa chân / dưới tay
    holes = ndimage.binary_fill_holes(core) & ~core
    hl, hn = ndimage.label(holes)
    for k in range(1, hn + 1):
        m = hl == k
        if m.sum() <= 500 and (reddark[m].mean() > .3 or m.sum() <= 150): core |= m
    core = ndimage.binary_closing(core, iterations=1) | core
    ring = ndimage.binary_dilation(core, iterations=1) & ~core & (mx < 60)
    body = core | ring
    alpha = body.astype(float)
    alpha[fire & ~body] = 1
    full = ndimage.binary_fill_holes(body | fire)                       # điểm tối bị bao kín (mắt, bóng mặt) giữ nguyên
    alpha[full] = 1
    g = reddark & ~full
    alpha[g] = np.clip((dist[g] - 14) / 70, 0, 1) * np.clip((mx[g] - 35) / 60, 0, 1)
    full_img = Image.fromarray(np.dstack([a, alpha * 255]).astype(np.uint8), 'RGBA')
    bo = ndimage.binary_fill_holes(body)                                 # chỉ người, bỏ lửa
    body_img = Image.fromarray(np.dstack([a, bo * 255]).astype(np.uint8), 'RGBA')
    return full_img, body_img

FW, FH = 168, 158
frames, bodies = [], []
for i in range(4):                 # 4 tư thế nhân vật (kèm lửa trên tay đúng như hình)
    fi, bi = cutout_char(card(i)); frames.append(fi); bodies.append(bi)
frames.append(cutout(card(4)))     # 4 hỏa cầu bay
frames.append(cutout(card(6)))     # 5 nổ (thẻ 7, không có quái)
frames.append(cutout(card(7)))     # 6 tàn lửa
frames.append(bodies[3])           # 7 tư thế phóng, đã tách quả cầu (dùng khi cầu bay đi)
sheet = Image.new('RGBA', (FW * len(frames), FH))
for i, f in enumerate(frames):
    sheet.paste(f, (i * FW, 0))
sheet.save(f'{OUT}/hoa_cau_ref.png', optimize=True)

def feet(img):                     # điểm giữa hai bàn chân: đáy khối người
    a = np.array(img)[..., 3] > 200
    ys, xs = np.nonzero(a); yb = ys.max()
    sel = ys >= yb - 10
    return [int(round(xs[sel].mean())), int(yb)]
def core(img, right=False):        # tâm vùng lửa trắng/vàng sáng nhất
    a = np.array(img).astype(int)
    m = (a[..., 3] > 200) & (a[..., :3].min(-1) > 170) & (a[..., 0] > 230)
    ys, xs = np.nonzero(m)
    if right:
        sel = xs >= np.percentile(xs, 70); ys, xs = ys[sel], xs[sel]
    return [int(round(xs.mean())), int(round(ys.mean()))]
meta = {'fw': FW, 'fh': FH,
        'feet': [feet(bodies[0]), feet(bodies[1]), feet(bodies[2]), feet(bodies[3]), None, None, None, feet(bodies[3])],
        'fire': [None, core(frames[1], True), core(frames[2], True), core(frames[3], True), core(frames[4], True), core(frames[5]), None, None]}
def height(img):
    ys = np.nonzero(np.array(img)[..., 3] > 200)[0]; return ys.max() - ys.min()
h0 = height(bodies[0])
meta['scale'] = [round(h0 / height(b), 3) for b in bodies] + [1, 1, 1, round(h0 / height(bodies[3]), 3)]
a6 = np.array(frames[6])[..., 3] > 100; ys, xs = np.nonzero(a6); meta['fire'][6] = [int(xs.mean()), int(ys.mean())]
json.dump(meta, open(f'{OUT}/hoa_cau_ref.json', 'w'))
print(json.dumps(meta))
