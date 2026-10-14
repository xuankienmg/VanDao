"""Tách nhân vật theo cảnh giới từ ảnh thiết kế nền ô cờ giả (xám/trắng).
Nền: điểm trung tính (ít màu) trong dải sáng của ô cờ, nối với mép ảnh — viền đen nét pixel chặn lại nên
không ăn vào người. Vệt linh khí xanh nhạt và vòng phù văn nối với nền cũng bị bỏ (nền tự vẽ hiệu ứng).
Xuất vào thư mục img:
  hero_<cg>_stand.png  (hình đứng nét, cao 360px)      · hero_<cg>.png / _48.png / _24.png (sprite pixel, quay phải)
  med_<cg>.png         (hình ngồi thiền nét, cao 400px)
Dùng: python tools/cut_heroes.py tools wwwroot/img   (đọc tools/hero_*_design.png)"""
import sys
import numpy as np
from PIL import Image
from scipy import ndimage

SRC, OUT = sys.argv[1], sys.argv[2]

TOL = 14
def cutout(a, glow=True, extra_bg=None, keep=.02, tol=None, loose_n=30, aura_k=(18, 22)):
    """a: HxWx3 int. Trả mặt nạ người (True).
    Nền = điểm ít màu trong dải sáng của ô cờ (đo ở mép ảnh), nối với mép; túi kẹt bên trong chỉ coi là nền khi
    có đủ cả ô sáng lẫn ô tối (ô cờ thật), để không ăn vào áo xám cùng độ sáng."""
    mx, mn = a.max(-1), a.min(-1); v = a.mean(-1); sat = mx - mn
    e = np.concatenate([a[:6].reshape(-1, 3), a[-6:].reshape(-1, 3), a[:, :6].reshape(-1, 3), a[:, -6:].reshape(-1, 3)])
    e = e[(e.max(-1) - e.min(-1)) <= 20].mean(-1)
    lo, hi = np.percentile(e, 1) - 14, np.percentile(e, 99) + 14
    t1, t2 = np.percentile(e, 20), np.percentile(e, 80)              # 2 sắc độ ô cờ (chia 2 cụm)
    for _ in range(10):
        c = np.abs(e - t1) < np.abs(e - t2); t1, t2 = e[c].mean(), e[~c].mean()
    mid = (t1 + t2) / 2
    tol = TOL if tol is None else tol
    cand = (sat <= 20) & ((np.abs(v - t1) <= tol) | (np.abs(v - t2) <= tol))
    if extra_bg is not None: cand |= extra_bg
    lab, n = ndimage.label(cand)
    border = set(np.unique(np.concatenate([lab[0], lab[-1], lab[:, 0], lab[:, -1]]))) - {0}
    bg = np.isin(lab, list(border))
    dark, light = cand & (v < mid - 8), cand & (v > mid + 8)
    for i, sl in enumerate(ndimage.find_objects(lab), 1):
        if i in border or sl is None: continue
        mm = lab[sl] == i; d, l = (dark[sl] & mm).sum(), (light[sl] & mm).sum()
        if d >= 60 and l >= 60 and min(d, l) / max(d, l) > .25: bg[sl] |= mm
    if extra_bg is not None: bg |= extra_bg
    # ô cờ lệch sắc (bị ám màu, khử răng cưa) sát nền: lan thêm tối đa 30px qua điểm ít màu trong dải sáng ô cờ
    loose = (sat <= 22) & (v >= lo) & (v <= hi)
    for _ in range(loose_n):
        grow = ndimage.binary_dilation(bg) & loose & ~bg
        if not grow.any(): break
        bg |= grow
    if glow:   # vệt linh khí xanh nhạt / phù văn: sáng, ngả lam-lục, lan từ nền
        R, G, B = a[..., 0], a[..., 1], a[..., 2]
        aura = ((G - R > aura_k[0]) | (B - R > aura_k[1])) & (v > max(t1, t2) - 6) & (mn > 80)   # chỉ vệt sáng nhạt, không ăn vào áo lam / ngọc
        for _ in range(40):
            grow = ndimage.binary_dilation(bg) & aura & ~bg
            if not grow.any(): break
            bg |= grow
    fg = ~bg
    fg = ndimage.binary_opening(fg, iterations=1)
    lab2, n2 = ndimage.label(fg)
    if n2:
        s = ndimage.sum(fg, lab2, range(1, n2 + 1))
        fg = np.isin(lab2, [i + 1 for i, x in enumerate(s) if x >= s.max() * keep])
    return fg

def rgba(a, m):
    return Image.fromarray(np.dstack([a, m * 255]).astype(np.uint8))

def crop(im):
    al = np.array(im)[..., 3]; ys, xs = np.nonzero(al)
    return im.crop((xs.min(), ys.min(), xs.max() + 1, ys.max() + 1))

def fit_h(im, h):
    w = round(im.width * h / im.height)
    arr = np.array(im).astype(float); al = arr[..., 3:] / 255
    pm = Image.fromarray(np.concatenate([arr[..., :3] * al, al * 255], -1).round().astype(np.uint8))
    r = np.array(pm.resize((w, h), Image.LANCZOS)).astype(float)
    a2 = np.clip(r[..., 3:], 0, 255) / 255
    rgb = np.where(a2 > .004, r[..., :3] / np.maximum(a2, 1e-3), 0)
    return Image.fromarray(np.concatenate([np.clip(rgb, 0, 255), a2 * 255], -1).round().astype(np.uint8))

OL = np.array([11, 8, 6])
def sprite(im, S, pad=1):
    """sprite pixel SxS: thu nhỏ kiểu BOX (giữ màu), ngưỡng alpha, viền tối 1px, chân sát đáy, căn giữa."""
    sp = crop(im)
    s = min((S - 2 * pad) / sp.width, (S - 2 * pad) / sp.height)
    w, h = max(1, round(sp.width * s)), max(1, round(sp.height * s))
    arr = np.array(sp).astype(float); al = arr[..., 3:] / 255
    pm = Image.fromarray(np.concatenate([arr[..., :3] * al, al * 255], -1).astype(np.uint8)).resize((w, h), Image.BOX)
    r = np.array(pm).astype(float); a2 = r[..., 3] / 255
    rgb = np.where(a2[..., None] > 0, r[..., :3] / np.maximum(a2[..., None], 1e-6), 0)
    m = a2 >= .5
    can = np.zeros((S, S, 3)); cm = np.zeros((S, S), bool)
    x0, y0 = (S - w) // 2, S - pad - h
    can[y0:y0 + h, x0:x0 + w][m] = rgb[m]; cm[y0:y0 + h, x0:x0 + w] = m
    ring = ndimage.binary_dilation(cm) & ~cm; can[ring] = OL; cm |= ring
    return Image.fromarray(np.dstack([can, cm * 255]).astype(np.uint8))

def load(name):
    return np.array(Image.open(f'{SRC}/{name}').convert('RGB')).astype(int)

# ---- hình đứng (3 cảnh giới) · bỏ thanh kiếm bay khỏi sprite nhỏ ----
STAND = {'luyenkhi': ('hero_luyenkhi_design.png', None), 'trucco': ('hero_trucco_design.png', (700, 210, 760, 570)),
         'kimdan': ('hero_kimdan_design.png', None)}
for cg, (fn, sword) in STAND.items():
    a = load(fn); m = cutout(a)
    if sword:                                                            # kiếm bay tách rời: giữ trong ảnh nét
        x0, y0, x1, y1 = sword; sub = cutout(a, keep=.0)[y0:y1, x0:x1]
        lab, n = ndimage.label(sub)
        if n: sz = ndimage.sum(sub, lab, range(1, n + 1)); m[y0:y1, x0:x1] |= lab == (np.argmax(sz) + 1)
    hd = crop(rgba(a, m))
    fit_h(hd, 360).save(f'{OUT}/hero_{cg}_stand.png')
    sm = m.copy()
    if sword: x0, y0, x1, y1 = sword; sm[y0:y1, x0:x1] = False    # kiếm bay tách rời: không đưa vào sprite nhỏ
    sp = crop(rgba(a, sm))
    fit_h(sp, 360).save(f'{OUT}/hero_{cg}.png')
    sprite(sp, 48).save(f'{OUT}/hero_{cg}_48.png')
    sprite(sp, 24).save(f'{OUT}/hero_{cg}_24.png')

# ---- hình ngồi: Luyện Khí, Trúc Cơ từ bảng 4 người (bỏ chiếu, kiếm cắm, vòng phù văn); Kim Đan từ ảnh riêng ----
sheet = load('hero_med_design.png')
# khung người (x0, y0, x1, y1) · mẫu màu chiếu (vùng chiếu không bị người che) · chiếu bắt đầu từ y = MAT_Y
MED = {'luyenkhi': ((97, 335, 300, 690), (95, 612, 125, 668)), 'trucco': ((302, 335, 515, 690), (302, 620, 320, 668))}
MAT_Y = 585
def mat_mask(a, kind, y_off):
    """chiếu ngồi (bỏ đi): Luyện Khí chiếu cói vàng lục, Trúc Cơ chiếu ngọc — chọn theo sắc màu, chỉ ở dải dưới."""
    R, G, B = a[..., 0], a[..., 1], a[..., 2]; v = a.mean(-1)
    if kind == 'luyenkhi': m = (G > B + 22) & (R > B + 8) & (G >= R - 10)         # chiếu cói vàng lục + viền lục sẫm (áo lam xám không lọt)
    else: m = (G > R + 45) & (v > 105) & (np.abs(G - B) < 45)                    # ngọc sáng (áo ngọc tối không lọt)
    m = ndimage.binary_closing(m, iterations=2) & ~((G < 70) & (v < 70)) | (m & True)
    # viền tối của chiếu: điểm tối kề vùng chiếu, ngả lục
    border = ndimage.binary_dilation(m, iterations=3) & (G > R + 8) & (v < 90) & ~m
    m |= border
    m[:max(0, MAT_Y - y_off)] = False
    return m
mats = np.zeros(sheet.shape[:2], bool)
for cg, ((x0, y0, x1, y1), (sx0, sy0, sx1, sy1)) in MED.items():
    mats[y0:y1, x0:x1] |= mat_mask(sheet[y0:y1, x0:x1], cg, y0)
full = cutout(sheet, extra_bg=mats)                                   # tách cả bảng một lần (lưới ô cờ tính trên cả ảnh)
for cg, ((x0, y0, x1, y1), _) in MED.items():
    m = full[y0:y1, x0:x1].copy()
    if cg == 'luyenkhi':                                              # vòng phù văn xanh ngọc sát mép trái: người Luyện Khí không có màu này
        sub = sheet[y0:y1, x0:x1]; R, G, B = sub[..., 0], sub[..., 1], sub[..., 2]
        m &= ~ndimage.binary_dilation((G - R > 35) & (B - R > 35), iterations=2)
        v = sub.mean(-1); ghost = m & (B - R > 8) & (v > 118) & (sub.max(-1) - sub.min(-1) < 60)   # vệt sáng mờ của vòng phù văn còn bám mép trái
        gl, gn = ndimage.label(ghost)
        for i, sl in enumerate(ndimage.find_objects(gl), 1):
            if sl[1].start < 40: m[gl == i] = False
    lab, n = ndimage.label(m)                                         # chỉ giữ người (mảng lớn nhất) trong khung
    if n: sz = ndimage.sum(m, lab, range(1, n + 1)); m = np.isin(lab, [i + 1 for i, x in enumerate(sz) if x >= sz.max() * .02])
    fit_h(crop(rgba(sheet[y0:y1, x0:x1], m)), 400).save(f'{OUT}/med_{cg}.png')
a = load('hero_kimdan_med_design.png')
# Kim Đan: tóc bạc xám gần sắc ô cờ, vắt qua vòng phù văn → so sắc ô cờ chặt hơn, không lan rộng, chỉ bỏ vệt xanh đậm
base_m = cutout(a)                                                    # tách chuẩn: nền sạch nhưng mất bớt tóc bạc
loose_m = cutout(a, tol=8, loose_n=4, aura_k=(30, 34))               # tách nhẹ tay: giữ tóc nhưng sót ô cờ
R_, G_, B_ = a[..., 0], a[..., 1], a[..., 2]
hair = loose_m & (R_ - B_ >= 7) & ndimage.binary_dilation(base_m, iterations=60)   # tóc bạc ngả ấm, ô cờ thì xám trung tính
m = base_m | hair
lab, n = ndimage.label(m); sz = ndimage.sum(m, lab, range(1, n + 1)); m = np.isin(lab, [i + 1 for i, x in enumerate(sz) if x >= sz.max() * .01])
fit_h(crop(rgba(a, m)), 400).save(f'{OUT}/med_kimdan.png')
print('ok')

# ---- bảng khung trận đấu (cửa sổ lớn) theo cảnh giới: cùng bố cục hoa_cau_ref.png (8 khung 168×158) ----
# khung 0/7: đứng · 1–3: nhoài người tới + quả cầu lửa lớn dần trước tay · 4–6: hiệu ứng lửa giữ nguyên bản cũ.
FW, FH = 168, 158
FEET = [(76, 153), (70, 150), (38, 148), (33, 146), None, None, None, (33, 146)]
FIRE = [None, (111, 80), (126, 82), (144, 76), None, None, None, None]
SCALE = [1, 1.107, 1.069, 1.204, 1, 1, 1, 1.204]
LEAN = [0, -3, -5, -7, 0, 0, 0, 2]                      # độ nghiêng (âm = nhoài về phía quái)
HERO_H = 124                                            # chiều cao nhân vật khung 0 trong bản cũ (px)
base = Image.open(f'{OUT}/hoa_cau_ref.png').convert('RGBA')
def orb(size):
    r = size; yy, xx = np.mgrid[-r:r + 1, -r:r + 1]; d = np.sqrt(xx ** 2 + yy ** 2) / r
    al = np.clip(1 - d, 0, 1) ** .7
    col = np.stack([255 * np.ones_like(d), np.clip(110 + 160 * (1 - d), 0, 255), np.clip(30 + 225 * (1 - d * 1.6), 0, 255)], -1)
    return Image.fromarray(np.dstack([col, al * 255]).astype(np.uint8))
for cg in STAND:
    fig = crop(Image.open(f'{OUT}/hero_{cg}.png').convert('RGBA'))
    sheet = Image.new('RGBA', (FW * 8, FH), (0, 0, 0, 0))
    for i in range(8):
        if FEET[i] is None:
            sheet.paste(base.crop((i * FW, 0, (i + 1) * FW, FH)), (i * FW, 0)); continue
        h = round(HERO_H / SCALE[i]); f = fit_h(fig, h)
        if LEAN[i]: f = f.rotate(LEAN[i], resample=Image.BICUBIC, expand=True, center=(f.width // 2, f.height))
        f = crop(f)
        fx, fy = FEET[i]
        sheet.alpha_composite(f, (i * FW + fx - f.width // 2, fy - f.height))
        if FIRE[i]:
            o = orb([8, 12, 16][i - 1]); sheet.alpha_composite(o, (i * FW + FIRE[i][0] - o.width // 2, FIRE[i][1] - o.height // 2))
    sheet.save(f'{OUT}/hoa_cau_ref_{cg}.png')
print('battle sheets ok')
