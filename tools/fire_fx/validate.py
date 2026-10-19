"""Validate the authoritative fire sprite manifest and packed PNG assets."""
import argparse
import json
import math
from pathlib import Path
from PIL import Image

DEFAULT_MANIFEST = Path(__file__).with_name('manifest.json')
ROOT = Path(__file__).resolve().parents[2]
EXPECTED = {('hoa_cau', v): 'hoa_cau_' + v for v in ('small','medium','large')}
EXPECTED.update({(s,'default'):s for s in ('liet_diem','hoa_long','phan_thien','xich_diem_thien','than_hoa','hu_khong_hoa')})
PHASES = [[0,.25,0,3],[.25,.45,4,7],[.45,.60,8,11],[.60,.75,12,15]]

def number(value):
    return type(value) in (int,float) and math.isfinite(value)

def validate_manifest(entries):
    if not isinstance(entries,list) or len(entries) != 9:
        raise ValueError('manifest must be an array of exactly nine entries')
    seen = set()
    for e in entries:
        if not isinstance(e,dict):
            raise ValueError('manifest entry must be an object')
        skill, variant = e.get('skillId'), e.get('variant')
        if not isinstance(skill,str) or not isinstance(variant,str):
            raise ValueError('skillId and variant must be strings')
        key = (skill,variant)
        if key not in EXPECTED or key in seen:
            raise ValueError(f'invalid or duplicate skill/variant: {key}')
        seen.add(key)
        if e.get('src') != f'img/fx/fire/{EXPECTED[key]}.png':
            raise ValueError(f'{key}: invalid asset reference')
        for field,value in [('frameWidth',192),('frameHeight',192),('columns',4),('frameCount',16)]:
            if type(e.get(field)) is not int or e[field] != value:
                raise ValueError(f'{key}: {field} must be {value}')
        for field in ('anchorX','anchorY'):
            if not number(e.get(field)) or not 0 <= e[field] < 192:
                raise ValueError(f'{key}: {field} outside frame')
        if not number(e.get('scale')) or e['scale'] <= 0:
            raise ValueError(f'{key}: invalid scale')
        if e.get('motion') != ('projectile' if skill in ('hoa_cau','hoa_long') else 'ground'):
            raise ValueError(f'{key}: invalid motion')
        if e.get('phases') != PHASES:
            raise ValueError(f'{key}: invalid phases or frame references')
    return entries

def validate_image(im, label):
    if im.mode != 'RGBA' or im.size != (768,768):
        raise ValueError(f'{label}: expected RGBA 768x768')
    alpha = im.getchannel('A')
    for i in range(16):
        x,y = i % 4 * 192, i // 4 * 192
        cell = alpha.crop((x,y,x+192,y+192))
        bounds = cell.getbbox()
        if bounds is None:
            raise ValueError(f'{label}: empty frame {i}')
        if bounds[0] < 8 or bounds[1] < 8 or bounds[2] > 184 or bounds[3] > 184:
            raise ValueError(f'{label}: frame {i} needs at least 8px transparent padding')
        if cell.getextrema()[0] != 0:
            raise ValueError(f'{label}: frame {i} lacks transparency')

def validate_assets(entries, out):
    validate_manifest(entries)
    for e in entries:
        path = Path(out) / Path(e['src']).name
        try:
            with Image.open(path) as im:
                if im.format != 'PNG':
                    raise ValueError(f'{path}: expected PNG encoding')
                validate_image(im, str(path))
        except (OSError, SyntaxError) as exc:
            raise ValueError(f'{path}: missing or unreadable PNG: {exc}') from exc

def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--manifest', type=Path, default=DEFAULT_MANIFEST)
    parser.add_argument('--out', type=Path, default=ROOT / 'wwwroot/img/fx/fire')
    args = parser.parse_args()
    try:
        validate_assets(json.loads(args.manifest.read_text(encoding='utf-8')),args.out)
    except (ValueError,OSError) as exc:
        parser.exit(1,f'Validation failed: {exc}\n')
    print('Validated 9 RGBA sprite sheets, 144 frames, metadata and padding.')

if __name__ == '__main__':
    main()
