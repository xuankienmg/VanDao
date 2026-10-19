"""Procedural VFX sprite sheet: 'Thanh Liên Kiếm Khí' (jade qi lotus burst).
4x4 grid, 16 frames: gather swirl -> condense core + lotus seal -> burst -> dissipate.
Output: transparent RGBA PNG + black-bg preview + animated GIF.
"""
import numpy as np
from PIL import Image
from scipy.ndimage import map_coordinates, gaussian_filter

S = 256          # cell size
N = 16           # frames
COLS = 4
rng = np.random.default_rng(7)

yy, xx = np.mgrid[0:S, 0:S].astype(np.float32)
cx = cy = (S - 1) / 2
X = (xx - cx) / (S / 2)          # -1..1
Y = (yy - cy) / (S / 2)
R = np.sqrt(X**2 + Y**2) + 1e-6
TH = np.arctan2(Y, X)


def fbm(size, octaves=5, seed=0):
    r = np.random.default_rng(seed)
    out = np.zeros((size, size), np.float32)
    amp, tot = 1.0, 0.0
    for o in range(octaves):
        n = 4 * 2**o
        g = r.random((n, n)).astype(np.float32)
        up = np.array(Image.fromarray(g).resize((size, size), Image.BICUBIC))
        out += amp * up
        tot += amp
        amp *= 0.5
    out /= tot
    return (out - out.min()) / (out.max() - out.min())


NOISE = [fbm(512, 6, s) for s in range(3)]


def sample(noise, x, y, rot=0.0, scale=1.0, off=(0, 0)):
    c, s = np.cos(rot), np.sin(rot)
    u = (c * x - s * y) * scale
    v = (s * x + c * y) * scale
    u = (u * 0.5 + 0.5 + off[0]) * 511
    v = (v * 0.5 + 0.5 + off[1]) * 511
    return map_coordinates(noise, [v, u], order=1, mode="wrap")


def ss(a, b, x):
    t = np.clip((x - a) / (b - a), 0, 1)
    return t * t * (3 - 2 * t)


def gauss(d, w):
    return np.exp(-(d / w) ** 2)


def star(x, y, size, thin=0.012, angle=0.0, arms=2):
    """Lens-flare star: `arms` lines crossing through origin."""
    out = np.zeros_like(x)
    for k in range(arms):
        a = angle + k * np.pi / arms
        c, s = np.cos(a), np.sin(a)
        along = c * x + s * y
        perp = -s * x + c * y
        out += np.exp(-np.abs(along) / size) * np.exp(-(perp / thin) ** 2)
    return out


# Jade palette: deep teal -> jade -> cyan -> white (intensity mapped)
PAL_POS = np.array([0.0, 0.25, 0.5, 0.75, 1.0])
PAL_COL = np.array([
    [0.00, 0.10, 0.12],
    [0.02, 0.38, 0.36],
    [0.10, 0.80, 0.68],
    [0.55, 1.00, 0.92],
    [1.00, 1.00, 1.00],
])


def colorize(I):
    t = np.clip(I, 0, 1.2) / 1.2
    rgb = np.stack([np.interp(t, PAL_POS, PAL_COL[:, k]) for k in range(3)], -1)
    return rgb * np.clip(I, 0, None)[..., None] * 1.3


# Persistent sparks
NSP = 70
sp_ang = rng.uniform(0, 2 * np.pi, NSP)
sp_spd = rng.uniform(0.35, 1.0, NSP)
sp_sz = rng.uniform(0.010, 0.022, NSP)
sp_ph = rng.uniform(0, 2 * np.pi, NSP)


def frame(i):
    t = i / (N - 1)
    I = np.zeros((S, S), np.float32)          # main qi energy
    G = np.zeros((S, S), np.float32)          # gold sparks
    D = np.zeros((S, S), np.float32)          # dark smoke (subtracts light, adds alpha)
    n0 = sample(NOISE[0], X, Y, rot=i * 0.35, scale=1.3)
    n1 = sample(NOISE[1], X, Y, rot=-i * 0.2, scale=2.2, off=(0.1 * i, 0))

    # ---- Phase A: gathering swirl (frames 0-7) ----
    gather = (0.45 + 0.55 * ss(0.0, 0.2, t)) * (1 - ss(0.45, 0.6, t))
    if gather > 0:
        rad = 0.22 + 0.58 * ss(0.0, 0.3, t)
        twist = 4.5
        arms = 3
        sp = 0.5 + 0.5 * np.cos(arms * (TH + twist * np.log(R + 0.05)) - i * 0.9)
        sp = sp**3
        env = gauss(R / rad, 0.85) * (1 - gauss(R, 0.06 + 0.05 * t))
        wisp = ss(0.25, 0.75, n0)
        I += gather * (0.8 * sp * env * (0.3 + 1.0 * wisp) + 0.45 * env * ss(0.3, 0.8, n1))
        D += gather * 0.6 * env * ss(0.55, 0.85, n1) * (1 - sp)

    # ---- Phase B: condensed core + lotus seal (frames 3-8) ----
    seal = ss(0.15, 0.3, t) * (1 - ss(0.5, 0.58, t))
    if seal > 0:
        Rr = 0.30 + 0.05 * ss(0.2, 0.5, t)
        ring = gauss(R - Rr, 0.025) * (0.7 + 0.6 * n0)
        petals = 8
        lotus_r = Rr * (1.15 + 0.55 * np.abs(np.cos(petals / 2 * (TH + i * 0.12))) ** 0.6)
        lotus = gauss(R - lotus_r, 0.018) * ss(0.2, 0.32, t)
        inner = gauss(R - Rr * 0.6, 0.010) * (0.5 + 0.5 * np.cos(24 * TH - i)) ** 4  # rune ticks
        void = gauss(R, Rr * 0.85)                  # dark core inside ring
        I += seal * (1.3 * ring + 0.9 * lotus + 0.6 * inner)
        D += seal * 1.4 * void * ss(0.18, 0.35, t)
        I += seal * star(X, Y, 0.10 + 0.05 * t, 0.010, angle=np.pi / 4)

    # ---- Phase C: burst (frames 7-11) ----
    bt = (t - 0.45) / 0.30                     # 0..1 across burst
    if 0 <= bt <= 1.2:
        k = np.clip(bt, 0, 1)
        flash = np.exp(-((bt - 0.15) / 0.3) ** 2)
        # radial rays from angular noise
        ang_n = sample(NOISE[2], np.cos(TH) * 0.6, np.sin(TH) * 0.6, scale=1.0)
        rays = ss(0.5, 0.75, ang_n) ** 3
        reach = 0.35 + 0.65 * ss(0, 0.6, k)
        rays_r = rays * gauss(R / reach, 0.6) * (1 - k * 0.7) * ss(0.05, 0.2, R)
        # shockwave ellipse (flattened ring)
        sw_r = 0.2 + 0.75 * ss(0, 1, k)
        E = np.sqrt(X**2 + (Y / 0.38) ** 2)
        shock = gauss(E - sw_r, 0.04 + 0.05 * k) * (1 - ss(0.6, 1.1, bt))
        # round shock ring
        ring2 = gauss(R - sw_r * 0.85, 0.03 + 0.06 * k) * (0.5 + 0.8 * n0) * (1 - ss(0.5, 1.0, bt))
        # energy cloud
        cloud = gauss(R / (0.3 + 0.45 * k), 0.7) * (0.3 + 0.9 * ss(0.3, 0.8, n0))
        petals_fly = gauss(R - lotus_petal_r(k), 0.05) * (np.abs(np.cos(4 * (TH + 0.4 * k))) ** 6)
        I += 1.6 * rays_r + 1.1 * shock + 0.8 * ring2 + 0.45 * cloud * (1.1 - k * 0.6)
        I += 0.8 * petals_fly * (1 - ss(0.6, 1.0, bt))
        I += flash * (1.8 * gauss(R, 0.11) + 1.6 * star(X, Y, 0.45, 0.012, arms=2)
                      + 0.8 * star(X, Y, 0.25, 0.008, angle=np.pi / 4))
        D += 0.7 * cloud * ss(0.6, 0.9, n1) * k

    # ---- Phase D: dissipate (frames 10-15) ----
    dt = (t - 0.62) / 0.38
    if dt > 0:
        dt = min(dt, 1.0)
        thr = 0.15 + 0.45 * dt
        cloud = gauss(R / (0.65 + 0.15 * dt), 0.75)
        breakup = ss(thr, thr + 0.15, 0.6 * n0 + 0.4 * n1)
        swirl = (0.5 + 0.5 * np.cos(3 * (TH + 2.5 * R) + i * 0.4)) ** 2
        I += (1 - dt) ** 1.1 * cloud * breakup * (0.6 + 0.8 * swirl)
        D += (1 - dt) * 0.5 * cloud * breakup * ss(0.5, 0.8, n1)
        I += (1 - dt) * 0.6 * gauss(R, 0.08)

    # ---- Sparks (all frames, motion follows phase) ----
    if t < 0.45:      # spiral inward
        rr = 0.85 * (1 - ss(0, 0.45, t) * 0.5) * sp_spd
        aa = sp_ang + t * 6
        vis = ss(0.0, 0.15, t) * 0.8
    else:             # fly outward
        k = (t - 0.45) / 0.55
        rr = 0.15 + sp_spd * (0.2 + 0.75 * ss(0, 0.7, k))
        aa = sp_ang + 0.6 * k
        vis = 1.0 if k < 0.85 else 1 - 0.6 * (k - 0.85) / 0.15
    for j in range(NSP):
        px, py = rr[j] * np.cos(aa[j]), rr[j] * np.sin(aa[j]) * (0.85 if t > 0.45 else 1)
        if abs(px) > 0.95 or abs(py) > 0.95:
            continue
        tw = 0.6 + 0.4 * np.sin(sp_ph[j] + i * 1.7)
        dx, dy = X - px, Y - py
        G += vis * tw * (1.4 * gauss(np.sqrt(dx**2 + dy**2), sp_sz[j])
                         + 0.5 * star(dx, dy, sp_sz[j] * 2.5, 0.004))

    # soft edge fade so nothing touches the cell border
    edge = 1 - ss(0.78, 0.97, R)
    I *= edge
    G *= edge
    D *= edge

    I = gaussian_filter(I, 0.6)
    I = 1.3 * (1 - np.exp(-I * 1.1))          # tonemap: keep detail in bright burst
    rgb = colorize(I)
    G = gaussian_filter(G, 0.5)
    rgb += np.clip(G, 0, 1.5)[..., None] * np.array([1.0, 0.82, 0.45]) * 1.3
    # bloom
    rgb += gaussian_filter(rgb, (4, 4, 0)) * 0.6
    # dark smoke: darken + violet-teal tint, contributes alpha
    D = np.clip(gaussian_filter(D, 1.2), 0, 1)
    rgb *= (1 - 0.65 * D)[..., None]

    a_light = np.clip(rgb.max(-1), 0, 1)
    a = np.clip(a_light + 0.55 * D * (1 - a_light), 0, 1)
    col = np.where(a[..., None] > 1e-4, np.clip(rgb, 0, None) / np.maximum(a, 1e-4)[..., None], 0)
    smoke_col = np.array([0.02, 0.10, 0.12])
    w = (0.55 * D * (1 - a_light) / np.maximum(a, 1e-4))[..., None]
    col = col * (1 - w) + smoke_col * w
    col = np.clip(col, 0, 1)
    rgba = np.dstack([col, a])
    return (rgba * 255 + 0.5).astype(np.uint8)


def lotus_petal_r(k):
    return 0.35 + 0.55 * ss(0, 1, k)


frames = [frame(i) for i in range(N)]
sheet = Image.new("RGBA", (S * COLS, S * (N // COLS)), (0, 0, 0, 0))
for i, f in enumerate(frames):
    sheet.paste(Image.fromarray(f, "RGBA"), ((i % COLS) * S, (i // COLS) * S))

out = "/mnt/user-data/outputs"
sheet.save(f"{out}/jade_lotus_burst_4x4.png")
bg = Image.new("RGBA", sheet.size, (0, 0, 0, 255))
Image.alpha_composite(bg, sheet).convert("RGB").save(f"{out}/jade_lotus_burst_preview_black.png")

gif = []
for f in frames:
    b = Image.new("RGBA", (S, S), (0, 0, 0, 255))
    gif.append(Image.alpha_composite(b, Image.fromarray(f, "RGBA")).convert("RGB"))
gif[0].save(f"{out}/jade_lotus_burst.gif", save_all=True, append_images=gif[1:], duration=60, loop=0)
print("done")
