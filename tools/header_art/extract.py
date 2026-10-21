"""Deterministic cuts from the supplied UI reference, with no generated text."""
from pathlib import Path
import json
from PIL import Image
import numpy as np

root = Path(__file__).resolve().parents[2]
source = Path(__file__).with_name('reference.png')
out = root / 'wwwroot/img/header'
out.mkdir(parents=True, exist_ok=True)
image = Image.open(source).convert('RGB')
boxes = {
    'req': (288, 12, 324, 49), 'inv': (432, 12, 466, 49),
    'craft': (598, 12, 635, 49), 'map': (754, 12, 791, 49),
    'skill': (895, 12, 932, 49), 'pet': (1058, 10, 1096, 49),
}
for name, box in boxes.items():
    crop = image.crop(box).convert('RGBA')
    pixels = np.array(crop)
    light = pixels[:, :, :3].max(axis=2).astype(float)
    # Reference background is near-black; preserve bronze/gold icon pixels and antialias edges.
    pixels[:, :, 3] = np.clip((light - 40) / 42, 0, 1) * 255
    Image.fromarray(pixels).save(out / f'nav-{name}.png')
image.crop((1193, 0, 1233, 62)).save(out / 'header-tile.png')
image.crop((790, 63, 1586, 149)).save(out / 'header-scenery.webp', 'WEBP', quality=92)
image.crop((33, 74, 110, 147)).save(out / 'pet-emblem.png')
manifest = {'source': str(source.relative_to(root)), 'art_source': 'existing', 'icons': boxes,
            'headerTile': [1193, 0, 1233, 62], 'scenery': [790, 63, 1586, 149],
            'text': 'All interface labels remain live text, not cropped lettering.'}
(out / 'cuts.json').write_text(json.dumps(manifest, ensure_ascii=False, indent=2), encoding='utf-8')
print(f'Extracted {len(boxes)} icons and 3 header assets into {out}')
