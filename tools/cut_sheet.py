#!/usr/bin/env python3
"""Cắt bảng vật phẩm nền trắng/đơn sắc thành từng icon PNG nền trong suốt.

Dùng:  python3 cut_sheet.py SHEET.png OUT_DIR --names mu,ao,gang,... [--prefix hb_] [--size 128] [--pad 4]
        [--thresh 14] [--gap 2] [--pair-gap 40] [--min-area 800] [--preview preview.png]

Cách làm:
 1. Ước lượng màu nền từ viền ảnh; khoảng cách tới nền = độ "đậm" của điểm ảnh.
 2. Mặt nạ vật = điểm có khoảng cách > thresh; lỗ kín bên trong vật được lấp
    (vải trắng của áo/quần), trừ lỗ lớn trùng màu nền (lòng vòng cổ, lòng nhẫn).
 3. Gom mảnh: nối mảnh cách nhau ≤ gap px; mảnh nhỏ gắn vào món gần nhất; 2 mảnh cùng cỡ
    ngang hàng cách ≤ pair-gap px (đôi giày, đôi hộ uyển) → 1 món.
 4. Thứ tự món: theo hàng (trên → dưới), trong hàng trái → phải.
 5. Alpha mềm ở mép (chống răng cưa), khử viền trắng (un-premultiply theo nền),
    căn giữa vào ô vuông size×size.
"""
import argparse, os
import numpy as np
from PIL import Image
from scipy import ndimage as nd


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument('sheet'); ap.add_argument('out')
    ap.add_argument('--names', default='')
    ap.add_argument('--prefix', default='')
    ap.add_argument('--size', type=int, default=128)
    ap.add_argument('--pad', type=int, default=4)
    ap.add_argument('--thresh', type=float, default=14)
    ap.add_argument('--gap', type=int, default=2)
    ap.add_argument('--pair-gap', type=int, default=40)
    ap.add_argument('--min-area', type=int, default=800)
    ap.add_argument('--preview', default='')
    a = ap.parse_args()

    rgb = np.array(Image.open(a.sheet).convert('RGB')).astype(np.float32)
    H, W, _ = rgb.shape
    border = np.concatenate([rgb[0], rgb[-1], rgb[:, 0], rgb[:, -1]])
    bg = np.median(border, 0)
    dist = np.sqrt(((rgb - bg) ** 2).sum(-1))

    solid = dist > a.thresh
    solid = nd.binary_closing(solid, iterations=1)
    # lấp lỗ kín, trừ lỗ to trùng màu nền
    filled = nd.binary_fill_holes(solid)
    holes, nh = nd.label(filled & ~solid)
    if nh:
        area = nd.sum(np.ones_like(dist), holes, range(1, nh + 1))
        mean = nd.mean(dist, holes, range(1, nh + 1))
        keep_bg = [i + 1 for i in range(nh) if (area[i] > max(700, 4.5e-4 * H * W) and mean[i] < a.thresh * .35) or (area[i] > 80 and mean[i] < a.thresh * .15)]
        filled[np.isin(holes, keep_bg)] = False
    mask = filled

    groups, ng = nd.label(nd.binary_dilation(mask, iterations=a.gap) if a.gap > 0 else mask)
    groups = groups * mask
    sls = nd.find_objects(groups)
    info = {}
    for gi, sl in enumerate(sls, 1):
        if sl is None: continue
        ar = int((groups[sl] == gi).sum())
        info[gi] = [sl[0].start, sl[0].stop, sl[1].start, sl[1].stop, ar]
    parent = {g: g for g in info}
    def find(g):
        while parent[g] != g: g = parent[g]
        return g
    def merge(p, q):
        p, q = find(p), find(q)
        if p == q: return
        parent[q] = p
        A, B = info[p], info[q]
        info[p] = [min(A[0], B[0]), max(A[1], B[1]), min(A[2], B[2]), max(A[3], B[3]), A[4] + B[4]]
    big = [g for g in info if info[g][4] >= a.min_area]
    # mảnh nhỏ (tua, hạt châu rời) → gắn vào món lớn gần nhất trong vòng 3×gap+20 px
    for g in [g for g in info if info[g][4] < a.min_area]:
        y0, y1, x0, x1, _ = info[g]
        best = None
        for b in big:
            Y0, Y1, X0, X1, _ = info[b]
            d = max(0, Y0 - y1, y0 - Y1) + max(0, X0 - x1, x0 - X1)
            if d <= 3 * a.gap + 20 and (best is None or d < best[0]): best = (d, b)
        if best: merge(best[1], g)
    # món đôi (giày, hộ uyển): 2 mảnh cùng cỡ, ngang hàng, cách nhau ≤ pair-gap px
    def thumb(g):                                       # hình dáng chuẩn hóa 32×32 của một mảnh
        y0, y1, x0, x1, _ = info[g]
        return np.array(Image.fromarray(((groups[y0:y1, x0:x1] == g) * 255).astype(np.uint8)).resize((32, 32))) > 127
    def shape_iou(p, q):                                # món đôi: 2 chiếc giống nhau (cho phép lật ngang)
        A, B = thumb(p), thumb(q)
        return max(((A & B).sum() / max(1, (A | B).sum())), ((A & B[:, ::-1]).sum() / max(1, (A | B[:, ::-1]).sum())))
    roots = sorted({find(g) for g in big})
    cands = []
    for i in range(len(roots)):
        for j in range(i + 1, len(roots)):
            A, B = info[roots[i]], info[roots[j]]
            hA, hB = A[1] - A[0], B[1] - B[0]
            ov = min(A[1], B[1]) - max(A[0], B[0])
            xg = max(B[2] - A[3], A[2] - B[3])
            if (-.45 * min(A[3] - A[2], B[3] - B[2]) <= xg <= a.pair_gap and ov > .75 * min(hA, hB)
                    and min(hA, hB) > .75 * max(hA, hB) and min(A[4], B[4]) > .5 * max(A[4], B[4])):
                iou = shape_iou(roots[i], roots[j])
                if iou >= .6: cands.append((-iou, roots[i], roots[j]))
    paired = set()
    for _, p, q in sorted(cands):                      # mỗi mảnh chỉ ghép đôi 1 lần, ưu tiên cặp giống nhau nhất
        if p in paired or q in paired: continue
        paired |= {p, q}; merge(p, q)
    lab = np.zeros_like(groups)
    for g in info: lab[groups == g] = find(g)
    groups = lab
    objs = []
    for g in sorted({find(g) for g in info}):
        y0, y1, x0, x1, ar = info[g]
        if ar >= a.min_area: objs.append((g, y0, y1, x0, x1))
    # xếp theo hàng: món cùng hàng nếu tâm dọc chồng lên khoảng dọc của món đầu hàng
    objs.sort(key=lambda o: (o[1] + o[2]) / 2)
    rows = []
    for o in objs:
        cy = (o[1] + o[2]) / 2
        if rows and rows[-1][0][1] <= cy <= rows[-1][0][2]: rows[-1].append(o)
        else: rows.append([o])
    order = [o for r in rows for o in sorted(r, key=lambda o: o[3])]

    names = [n for n in a.names.split(',') if n] if a.names else []
    os.makedirs(a.out, exist_ok=True)
    # alpha mềm: 0 tại nền → 255 khi khoảng cách ≥ 3×thresh; trong mặt nạ đã lấp → 255
    soft = np.clip((dist - a.thresh * .4) / (a.thresh * 2.6), 0, 1)
    tiles = []
    for k, (gi, y0, y1, x0, x1) in enumerate(order):
        name = names[k] if k < len(names) else f'item_{k + 1:02d}'
        if name in ('-', '_', 'skip'): continue
        sl = (slice(y0, y1), slice(x0, x1))
        own = groups[sl] == gi
        inner = nd.binary_erosion(mask[sl], iterations=2)
        al = np.where(inner, 1.0, soft[sl]) * own
        al[~nd.binary_dilation(mask[sl], iterations=2)] = 0
        c = rgb[sl]
        # khử viền nền: C = (P - (1-α)·bg) / α
        aa = np.maximum(al, 1e-3)[..., None]
        col = np.clip((c - (1 - aa) * bg) / aa, 0, 255)
        col = np.where(al[..., None] > .98, c, col)
        img = Image.fromarray(np.dstack([col, al * 255]).astype(np.uint8), 'RGBA')
        S = a.size - 2 * a.pad
        w, h = img.size; s = S / max(w, h)
        img = img.resize((max(1, round(w * s)), max(1, round(h * s))), Image.LANCZOS)
        out = Image.new('RGBA', (a.size, a.size), (0, 0, 0, 0))
        out.paste(img, ((a.size - img.width) // 2, (a.size - img.height) // 2), img)
        path = os.path.join(a.out, f'{a.prefix}{name}.png'); out.save(path)
        tiles.append((name, out)); print(f'{k + 1:2d}. {name:12s} bbox=({x0},{y0})-({x1},{y1}) -> {path}')

    if a.preview and tiles:
        n = len(tiles); cols = min(6, n); r = (n + cols - 1) // cols; T = a.size * 2
        pv = Image.new('RGBA', (cols * T, r * T * 2), (0, 0, 0, 255))
        for i, (_, t) in enumerate(tiles):
            big = t.resize((T, T), Image.LANCZOS)
            x, y = (i % cols) * T, (i // cols) * T * 2
            pv.paste((30, 30, 40, 255), (x, y, x + T, y + T)); pv.alpha_composite(big, (x, y))
            pv.paste((240, 235, 220, 255), (x, y + T, x + T, y + 2 * T)); pv.alpha_composite(big, (x, y + T))
        pv.save(a.preview)


if __name__ == '__main__':
    main()
