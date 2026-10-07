"""Tách tư thế ngồi thiền từ ảnh thiết kế: bỏ nền tối, vòng trận pháp, dấu sao Gemini; bỏ quầng sáng trắng.
Dùng: python tools/cut_med.py <ảnh> <ra.png>"""
import sys
import numpy as np
from PIL import Image
from scipy import ndimage
src, out = sys.argv[1], sys.argv[2]
a = np.array(Image.open(src).convert('RGB')).astype(float)
H, W, _ = a.shape
R, G, B = a[..., 0], a[..., 1], a[..., 2]
mx, mn = a.max(-1), a.min(-1)
BG = np.median(np.concatenate([a[:20].reshape(-1, 3), a[-20:].reshape(-1, 3)]), 0)
dist = np.sqrt(((a - BG) ** 2).sum(-1))
teal = (G - R > 12) & (G > B - 25)                      # chấm vòng trận pháp (và quầng của nó)
grey = (mx - mn <= 10)
strong = (dist > 25) & ~teal & ~grey                    # màu nhân vật rõ ràng
lab, n = ndimage.label(strong); sizes = ndimage.sum(strong, lab, range(1, n + 1))
strong = np.isin(lab, [i + 1 for i, v in enumerate(sizes) if v >= 150])
strong = ndimage.binary_closing(strong, iterations=3) | strong
solid = ndimage.binary_fill_holes(strong)
glow = grey & (mx > 32)                                  # quầng sáng trắng xám quanh người
solid = ndimage.binary_fill_holes(ndimage.binary_dilation(solid, iterations=4) & (dist > 4) & ~glow | solid)   # viền tối + vùng áo tối, bỏ quầng
lab3, n3 = ndimage.label(solid); sz3 = ndimage.sum(solid, lab3, range(1, n3 + 1))
solid = np.isin(lab3, [i + 1 for i, v in enumerate(sz3) if v >= 0.004 * sz3.max()])   # bỏ mảnh vụn rời
alpha = solid.astype(float)
alpha[teal & ~solid] = 0
near = ndimage.binary_dilation(solid, iterations=28)    # quầng sáng chỉ giữ sát người (bỏ dấu sao, mảnh xa)
alpha[~near] = 0
am = np.maximum(alpha, 1e-3)[..., None]
col = np.where(solid[..., None], a, np.clip((a - BG * (1 - am)) / am, 0, 255))
img = Image.fromarray(np.dstack([col, alpha * 255]).astype(np.uint8), 'RGBA')
bb = img.getbbox(); img = img.crop(bb)
img.save(out, optimize=True); print(bb, img.size)
