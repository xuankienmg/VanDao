"""Cắt bộ trang bị Thiên cấp từ ảnh thiết kế (nền lam + quầng sáng): GrabCut từng món, bỏ quầng/khói,
xoay kiếm về chéo như icon cũ, xuất icon 128×128 kèm hào quang vàng kim → wwwroot/img/thien_*.png.
Dùng: python tools/cut_thien.py tools/thien_design.png wwwroot/img"""
import numpy as np, cv2, json
from PIL import Image
from scipy import ndimage
import sys, os
SRC, OUTDIR = sys.argv[1], sys.argv[2]
A = np.array(Image.open(SRC).convert('RGB'))
CUT = {}
BOX = {  # vùng cắt (x0,y0,x1,y1) và khung grabcut tương đối (lề)
 'mu':    (50, 15, 215, 245),  'ao': (275, 15, 480, 245), 'quan': (555, 20, 715, 258), 'giay': (770, 15, 990, 222),
 'gang':  (25, 258, 260, 432), 'nhan': (300, 268, 485, 466), 'kiem': (30, 432, 505, 538),
 'pbkiem': (555, 280, 765, 535), 'chau': (775, 282, 1000, 538),
}
S = 2
for k, (x0, y0, x1, y1) in BOX.items():
    c = np.ascontiguousarray(A[y0:y1, x0:x1]); big = cv2.resize(c, None, fx=S, fy=S, interpolation=cv2.INTER_CUBIC)
    m = np.zeros(big.shape[:2], np.uint8); b1 = np.zeros((1, 65)); f1 = np.zeros((1, 65))
    pad = 6 * S
    cv2.grabCut(big[..., ::-1].copy(), m, (pad, pad, big.shape[1] - 2 * pad, big.shape[0] - 2 * pad), b1, f1, 8, cv2.GC_INIT_WITH_RECT)
    fg = (m == 1) | (m == 3)
    # quầng sáng xanh lơ / lấp lánh: rất sáng, ngả lam, ít chi tiết → bỏ
    r_, g_, b_ = [big[..., i].astype(int) for i in range(3)]
    glow = (b_ > 150) & (b_ - r_ > 60) & (g_ > 110)
    if k != 'nhan': fg &= ~ndimage.binary_opening(glow, iterations=1)
    if k == 'pbkiem':                                   # dải khói xanh thẫm quấn quanh kiếm
        wisp = (b_ - r_ > 25) & (r_ < 110) & (g_ > r_ + 10)
        wisp = ndimage.binary_opening(wisp, iterations=1)
        core = fg & ~wisp; ys, xs = np.nonzero(core)
        pts = np.stack([xs, ys], 1).astype(float); mu = pts.mean(0); _, _, vt = np.linalg.svd(pts - mu, full_matrices=False)
        yy, xx = np.mgrid[:fg.shape[0], :fg.shape[1]]; d = np.abs((xx - mu[0]) * vt[0][1] - (yy - mu[1]) * vt[0][0])
        fg &= ~(wisp & (d > 16 * S))                    # chỉ bỏ khói xa trục kiếm, giữ chuôi
    if k == 'nhan':                                     # lòng nhẫn: nền xanh lơ lọt qua
        hole = (b_ - r_ > 45) & (r_ < 110)
        hole[: int(hole.shape[0] * .42)] = False         # mặt nhẫn (tinh không) ở nửa trên: giữ
        fg &= ~ndimage.binary_opening(hole, iterations=1)
    fg = ndimage.binary_opening(fg, iterations=1)
    lab, n = ndimage.label(fg); sz = ndimage.sum(fg, lab, range(1, n + 1))
    fg = np.isin(lab, [i + 1 for i, z in enumerate(sz) if z > sz.max() * .08])
    fg = ndimage.binary_fill_holes(fg) if k not in ('nhan', 'chau') else fg
    CUT[k] = (big, fg)

GOLD = np.array([242, 201, 76])
def icon(k, rot=0):
    big, m = CUT[k]; m = m.astype(np.float32)
    m = cv2.GaussianBlur(m, (3, 3), 0)
    rgba = np.dstack([big, m * 255]).astype(np.uint8)
    im = Image.fromarray(rgba)
    if rot: im = im.rotate(rot, resample=Image.BICUBIC, expand=True)
    a = np.array(im).astype(float); ys, xs = np.nonzero(a[..., 3] > 128)
    a = a[ys.min():ys.max() + 1, xs.min():xs.max() + 1]
    al = a[..., 3:] / 255; pm = np.concatenate([a[..., :3] * al, al * 255], -1)
    H, W = pm.shape[:2]; s = 112 / max(H, W); w, h = max(1, round(W * s)), max(1, round(H * s))
    r = cv2.resize(pm, (w, h), interpolation=cv2.INTER_AREA)
    canvas = np.zeros((128, 128, 4)); ox, oy = (128 - w) // 2, (128 - h) // 2; canvas[oy:oy + h, ox:ox + w] = r
    # hào quang vàng kim (Thiên cấp): viền mờ phía sau món đồ
    alpha = canvas[..., 3] / 255
    glow = cv2.GaussianBlur((alpha > .3).astype(np.float32), (0, 0), 3.5) * .75
    ga = np.clip(glow, 0, 1)[..., None]
    pm_item = canvas[..., :3]                       # đã nhân alpha
    out_rgb = pm_item + GOLD * ga * (1 - alpha[..., None])
    out_a = alpha + ga[..., 0] * (1 - alpha)
    rgb = np.where(out_a[..., None] > .004, out_rgb / np.maximum(out_a[..., None], 1e-3), 0)
    return Image.fromarray(np.dstack([np.clip(rgb, 0, 255), out_a * 255]).round().astype(np.uint8))
JOBS = {'thien_mu': ('mu', 0), 'thien_ao': ('ao', 0), 'thien_quan': ('quan', 0), 'thien_giay': ('giay', 0), 'thien_gang': ('gang', 0),
        'thien_nhan': ('nhan', 0), 'thien_kiem': ('kiem', 45), 'thien_phi_kiem': ('pbkiem', -12), 'thien_chau': ('chau', 0)}
for name, (k, rot) in JOBS.items(): icon(k, rot).save(os.path.join(OUTDIR, f'{name}.png'))
