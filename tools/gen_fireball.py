"""Sinh sprite sheet pixel-art cho kỹ năng Hỏa Cầu (VanDao).

fx_hoa_cau_anim.png : 18 khung x 3 hàng (cấp 1-2 Nhỏ, 3-4 Trung, 5 Lớn), mỗi khung 64x64.
  0-2   tụ hỏa (lửa nhóm trên tay)        anchor = bàn tay
  3-5   hình thành hỏa cầu (xoáy thu lại)  anchor = bàn tay
  6     phóng ra (lóe lửa phía sau)        anchor = tâm cầu
  7-10  bay (vòng lặp, có đuôi lửa)        anchor = tâm cầu
  11-13 va chạm (lóe trắng -> nổ hình sao)  anchor = tâm nổ
  14-15 tan biến (vỡ vụn, khói)
  16-17 kết thúc (tàn lửa rơi)
fx_hoa_cau.png      : bản 48px 4x2 cũ (giữ bố cục để các chiêu khác dùng chung), vẽ lại cùng phong cách.
"""
import math, sys
import numpy as np
from PIL import Image

PAL = [(0x3a, 0x0c, 0x08), (0x7a, 0x1a, 0x10), (0xb0, 0x2a, 0x1a), (0xe0, 0x52, 0x1e),
       (0xf0, 0x8a, 0x2a), (0xf8, 0xd2, 0x5a), (0xff, 0xf4, 0xd0)]
SMOKE = [(0x2e, 0x26, 0x22), (0x4a, 0x3a, 0x34), (0x6a, 0x58, 0x4e)]


def vnoise(n, cells, seed):
    rng = np.random.default_rng(seed)
    grid = rng.random((cells + 2, cells + 2))
    xs = np.linspace(0, cells, n, endpoint=False)
    i = xs.astype(int); f = xs - i; f = f * f * (3 - 2 * f)
    a = grid[np.ix_(i, i)]; b = grid[np.ix_(i, i + 1)]; c = grid[np.ix_(i + 1, i)]; d = grid[np.ix_(i + 1, i + 1)]
    fy = f[:, None]; fx = f[None, :]
    return (a * (1 - fx) + b * fx) * (1 - fy) + (c * (1 - fx) + d * fx) * fy


def fbm(n, seed, base=4):
    return (vnoise(n, base, seed) * .55 + vnoise(n, base * 2, seed + 1) * .3 + vnoise(n, base * 4, seed + 2) * .15)


class Frame:
    def __init__(self, n):
        self.n = n
        self.I = np.zeros((n, n))          # cường độ lửa 0..1
        self.dots = []                     # (x, y, rgb, alpha)
        self.smoke = np.zeros((n, n))
        yy, xx = np.mgrid[0:n, 0:n]
        self.x = xx + .5 - n / 2; self.y = yy + .5 - n / 2

    def add(self, field):
        self.I = np.maximum(self.I, np.clip(field, 0, 1.2))

    def dot(self, x, y, lvl, a=255):
        self.dots.append((x, y, PAL[lvl] if isinstance(lvl, int) else lvl, a))

    def render(self, glow=True):
        n = self.n; img = np.zeros((n, n, 4), np.uint8)
        filled = self.I > .1
        # quầng sáng mờ quanh khối lửa
        if glow and filled.any():
            m = filled.copy()
            for r, a in ((2, 40), (1, 90)):
                dil = np.zeros_like(m)
                for dy in range(-r, r + 1):
                    for dx in range(-r, r + 1):
                        if dx * dx + dy * dy <= r * r + 1:
                            dil |= np.roll(np.roll(m, dy, 0), dx, 1)
                ring = dil & ~filled
                img[ring] = (*PAL[2], a)
        sm = (self.smoke > .15) & ~filled
        lv = np.clip((self.smoke * 3).astype(int), 0, 2)
        for k in range(3):
            sel = sm & (lv == k)
            img[sel] = (*SMOKE[k], 200)
        q = np.clip((self.I * 6.2).astype(int), 0, 6)
        for k in range(7):
            sel = filled & (q == k)
            img[sel] = (*PAL[max(1, k)], 255)
        # viền tối 1px ngoài cùng của khối lửa (đặt lên quầng)
        edge = filled & ~(np.roll(filled, 1, 0) & np.roll(filled, -1, 0) & np.roll(filled, 1, 1) & np.roll(filled, -1, 1))
        img[edge & (q <= 2)] = (*PAL[1], 255)
        for x, y, c, a in self.dots:
            xi, yi = int(round(x + n / 2)), int(round(y + n / 2))
            if 0 <= xi < n and 0 <= yi < n:
                img[yi, xi] = (*c, a)
        return Image.fromarray(img, 'RGBA')


TIERS = [  # bán kính cầu, độ dài đuôi, bán kính nổ, số tia, số tia lửa
    dict(R=5.5, L=20, X=17, rays=8, sp=10),
    dict(R=7.0, L=26, X=23, rays=10, sp=16),
    dict(R=9.0, L=30, X=29, rays=13, sp=24),
]


def ball(F, cx, cy, R, seed, heat=1.0):
    x, y = F.x - cx, F.y - cy
    r = np.hypot(x, y); a = np.arctan2(y, x)
    nz = fbm(F.n, seed, 3)
    edge = R * (1 + .06 * np.sin(a * 3 + seed) + .22 * (nz - .5))
    F.add((1 - r / edge) * 1.6 * heat + .15)
    F.I[(r < edge) & (F.I < .2)] = .2


def trail(F, cx, cy, R, L, seed, spread=1.0):
    x, y = F.x - cx, F.y - cy
    nz = fbm(F.n, seed + 7, 4)
    t = np.clip(-x / L, 0, 1)                       # 0 tại cầu -> 1 cuối đuôi
    wob = np.sin(-x * .35 + seed * 1.7) * 1.4 * t
    w = R * (1.05 - .95 * t ** .8) * spread
    base = (1 - np.abs(y - wob) / np.maximum(w, .3)) * (1 - t) ** .7 * 1.15
    f = base + (nz - .5) * .7 * (base > 0)
    f[x > 0] = 0
    F.add(f)
    # vài lưỡi lửa tách khỏi đuôi
    rng = np.random.default_rng(seed)
    for _ in range(int(L / 5)):
        d = rng.uniform(.3, 1.1) * L
        F.dot(cx - d, cy + rng.normal(0, R * .6), int(rng.integers(2, 5)))


def sparks(F, cx, cy, rmin, rmax, k, seed, lv=(3, 6)):
    rng = np.random.default_rng(seed)
    for _ in range(k):
        a = rng.uniform(0, 2 * math.pi); r = rng.uniform(rmin, rmax)
        F.dot(cx + math.cos(a) * r, cy + math.sin(a) * r, int(rng.integers(*lv)))


def star(F, cx, cy, Rx, rays, seed, heat=1.0, holes=0.0):
    rng = np.random.default_rng(seed)
    x, y = F.x - cx, F.y - cy
    r = np.hypot(x, y); a = np.arctan2(y, x)
    sp = np.zeros_like(a)
    off = rng.uniform(0, 2 * math.pi)
    for k in range(rays):
        th = off + 2 * math.pi * k / rays + rng.uniform(-.25, .25); s = rng.uniform(.55, 1)
        d = np.angle(np.exp(1j * (a - th)))
        sp = np.maximum(sp, s * np.exp(-(d / .16) ** 2))
    nz = fbm(F.n, seed, 4)
    edge = Rx * (.42 + .58 * sp) * (.9 + .25 * nz)
    base = (1 - r / edge) * 1.5 * heat
    f = base + (nz - .5) * .3 * (base > 0)
    if holes:
        hn = fbm(F.n, seed + 33, 6)
        f = np.where(hn < holes, -1, f - holes * .6)
        sn = fbm(F.n, seed + 44, 8)
        F.smoke = np.maximum(F.smoke, np.where((r < edge * 1.1) & (hn < holes) & (sn > .5), (sn - .3) * 1.2, 0))
    F.add(f)


def make_frames(n, tier):
    T = TIERS[tier]; R, L, X = T['R'], T['L'], T['X']
    out = []
    s0 = 100 * tier
    # 0-2 tụ hỏa: ngọn lửa hình giọt nước trên tay, lớn dần
    for i, k in enumerate((.45, .7, 1.0)):
        F = Frame(n)
        rw, rh = (R * .7) * k + 1, (R * 1.4) * k + 1.5
        x, y = F.x, F.y + rh * .45
        tt = np.clip((-y / rh + 1) / 2, 0, 1)
        nz = fbm(n, s0 + i, 5)
        base = (1 - np.sqrt((x / (rw * (1.1 - .8 * tt))) ** 2 + (y / rh) ** 2)) * 1.6
        f = base + (nz - .5) * .5 * (base > 0)
        F.add(f)
        sparks(F, 0, -rh * .6, rh * .6, rh * 1.6 + 3, 3 + i * 2, s0 + 10 + i)
        for j in range(4):                             # linh khí xanh tụ về tay
            a = j * math.pi / 2 + i * .5; r = (R * 2.4 + 6) * (1 - k * .6)
            F.dot(math.cos(a) * r, math.sin(a) * r, (0x6f, 0xe0, 0xe0))
        out.append(F)
    # 3-5 hình thành: xoáy lửa thu lại thành cầu
    for i, k in enumerate((.45, .75, 1.0)):
        F = Frame(n)
        ball(F, 0, 0, R * k, s0 + 20 + i, heat=.8 + .2 * k)
        ringR = R * (2.3 - 1.0 * k) + 2
        r = np.hypot(F.x, F.y); a = np.arctan2(F.y, F.x)
        for arm in range(3):
            ph = arm * 2 * math.pi / 3 + i * 1.3
            d = np.angle(np.exp(1j * (a - ph)))
            seg = (d > 0) & (d < 1.9)
            rr = ringR - d * 1.2
            band = 1 - np.abs(r - rr) / 1.3
            F.add(np.where(seg, band * (1.0 - d / 2.4) * 1.1, 0))
        sparks(F, 0, 0, ringR, ringR + 4, 4 + 2 * tier, s0 + 30 + i)
        out.append(F)
    # 6 phóng ra: cầu + lóe lửa hình nón phía sau
    F = Frame(n)
    ball(F, 2, 0, R * 1.05, s0 + 40, 1.1)
    x, y = F.x - 2, F.y
    cone = (1 - np.abs(y) / (np.clip(-x, 0, None) * .55 + 1)) * np.clip(1 + x / (L * .6), 0, 1) * 1.2
    F.add(np.where((x < 0) & (cone > 0), cone + (fbm(n, s0 + 41, 5) - .5) * .5, 0))
    sparks(F, -L * .3, 0, 2, L * .5, 6 + tier * 3, s0 + 42)
    out.append(F)
    # 7-10 bay: vòng lặp 4 khung
    for i in range(4):
        F = Frame(n)
        cx = n / 2 - R - 4
        cx = min(8, cx)
        trail(F, cx, 0, R, L, s0 + 50 + i)
        ball(F, cx, 0, R, s0 + 60 + i, 1.05)
        out.append(F)
    # 11 va chạm: lóe trắng
    F = Frame(n)
    star(F, 0, 0, X * .55, T['rays'], s0 + 70, heat=1.6)
    sparks(F, 0, 0, X * .4, X * .7, T['sp'] // 2, s0 + 71, (4, 7))
    out.append(F)
    # 12-13 nổ hình sao lớn dần
    for i, k in enumerate((.8, 1.0)):
        F = Frame(n)
        star(F, 0, 0, X * k, T['rays'], s0 + 70, heat=1.25 - .1 * i)
        sparks(F, 0, 0, X * k * .8, X * k * 1.08, T['sp'], s0 + 72 + i)
        out.append(F)
    # 14-15 tan biến
    for i, h in enumerate((.35, .55)):
        F = Frame(n)
        star(F, 0, 0, X * (1.02 + .05 * i), T['rays'], s0 + 70, heat=.75 - .2 * i, holes=h)
        sparks(F, 0, 0, X * .9, X * 1.1, T['sp'], s0 + 74 + i, (2, 5))
        out.append(F)
    # 16-17 kết thúc: tàn lửa rơi + khói mỏng
    for i in range(2):
        F = Frame(n)
        rng = np.random.default_rng(s0 + 80)
        for j in range(T['sp'] + 6):
            a = rng.uniform(0, 2 * math.pi); r = rng.uniform(.3, 1) * X
            px, py = math.cos(a) * r, math.sin(a) * r * .8 + (i + 1) * 3 + j % 3
            if rng.random() < (.75 if i == 0 else .45):
                F.dot(px, py, int(rng.integers(1, 5)) if i else int(rng.integers(2, 6)))
            elif rng.random() < .5:
                F.dot(px, py - 4, SMOKE[int(rng.integers(0, 3))], 180)
        out.append(F)
    return out


def build(outdir):
    N, COLS = 64, 18
    sheet = Image.new('RGBA', (N * COLS, N * 3))
    for tier in range(3):
        for i, F in enumerate(make_frames(N, tier)):
            sheet.paste(F.render(), (i * N, tier * N))
    sheet.save(f'{outdir}/fx_hoa_cau_anim.png', optimize=True)
    # bản 48px 4x2 dùng chung (bố cục cũ: 0 tia lửa,1 lửa tay,2 cầu,3 cầu+đuôi,4 đuôi dài,5 lóe,6 nổ,7 tàn)
    fr = make_frames(48, 1)
    pick = [0, 2, 5, 6, 8, 11, 13, 16]
    legacy = Image.new('RGBA', (192, 96))
    for j, i in enumerate(pick):
        legacy.paste(fr[i].render(), ((j % 4) * 48, (j // 4) * 48))
    legacy.save(f'{outdir}/fx_hoa_cau.png', optimize=True)


if __name__ == '__main__':
    build(sys.argv[1] if len(sys.argv) > 1 else '.')
