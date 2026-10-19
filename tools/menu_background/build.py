"""Package the generated plate without changing its artwork."""
from pathlib import Path
from PIL import Image
import hashlib
import json

root = Path(__file__).resolve().parents[2]
source = Path(__file__).with_name('source.png')
target = root / 'wwwroot/img/backgrounds/menu-night-v1.webp'
target.parent.mkdir(parents=True, exist_ok=True)
with Image.open(source) as image:
    image.convert('RGB').save(target, 'WEBP', quality=86, method=6)
    size = list(image.size)
report = {
    'art_source': 'host_image', 'route': 'image_gen', 'model': 'host tool default',
    'estimateUsd': None, 'estimateNote': 'Host image tool did not report a cost estimate',
    'source': 'tools/menu_background/source.png', 'asset': target.relative_to(root).as_posix(),
    'pixels': size, 'bytes': target.stat().st_size,
    'sha256': hashlib.sha256(target.read_bytes()).hexdigest(),
    'conversion': 'WebP quality 86; no resizing, recoloring or artwork edits'
}
Path(__file__).with_name('asset.json').write_text(json.dumps(report, ensure_ascii=False, indent=2), encoding='utf-8')
print(json.dumps(report))
