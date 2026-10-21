"""Ownership crops for three evolution stills; boundaries lie in empty magenta gutters."""
from pathlib import Path
from PIL import Image
import json, subprocess, sys, shutil

root = Path(__file__).resolve().parents[2]
source = root / 'tools/pet_redesign/thanh-long-source.png'
work = root / 'obj/thanh-long-final'
work.mkdir(exist_ok=True)
image = Image.open(source).convert('RGB')
processor = Path.home() / '.codex/skills/generate2dsprite/scripts/generate2dsprite.py'
regions = [(0, 490), (490, 1008), (1008, 1536)]
reports = []
for form, (left, right) in enumerate(regions):
    crop = image.crop((left, 0, right, image.height))
    padded = Image.new('RGB', (600, 1100), '#ff00ff')
    padded.paste(crop, ((600-crop.width)//2, 38))
    raw = work / f'form-{form}.png'
    padded.save(raw)
    out = work / f'processed-{form}'
    subprocess.run([sys.executable, str(processor), 'process', '--input', str(raw),
                    '--target', 'asset', '--mode', 'single', '--single-size', '512',
                    '--background-mode', 'chroma_key', '--key-quality', 'soft',
                    '--despill-radius', '1', '--resampler', 'lanczos',
                    '--trim-border', '0', '--edge-clean-depth', '0',
                    '--strict-qc', '--output-dir', str(out)], check=True)
    meta = json.loads((out / 'pipeline-meta.json').read_text())
    reports.append({'form':form, 'source_region':[left,0,right,image.height], 'qa':meta['qa']})
    candidates = [p for p in out.glob('*.png') if p.name not in ['raw-sheet.png','raw-sheet-clean.png','sheet-transparent.png']]
    art = Image.open(out / 'clean.png' if (out / 'clean.png').exists() else candidates[0]).convert('RGBA')
    bbox = art.getbbox()
    art = art.crop(bbox)
    for size, suffix in [(256,'_hd'), (128,'')]:
        thumb = art.copy(); thumb.thumbnail((size-8,size-8), Image.Resampling.LANCZOS)
        canvas = Image.new('RGBA',(size,size))
        canvas.alpha_composite(thumb,((size-thumb.width)//2,size-4-thumb.height))
        canvas.save(root / f'wwwroot/img/pet/thanh_long_loi_{form}{suffix}.png')
(root / 'tools/pet_redesign/art-qa.json').write_text(json.dumps(reports,indent=2),encoding='utf-8')
print('Published three lightning dragon stills at 256px and 128px.')
