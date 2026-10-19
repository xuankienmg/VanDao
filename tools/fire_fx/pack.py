"""Deterministically crop generated 4x4 RGBA grids; no artwork retouching."""
import argparse
import json
from pathlib import Path
from PIL import Image
from validate import DEFAULT_MANIFEST, ROOT, validate_manifest, validate_image


def package(entries, sources, out, runtime):
    validate_manifest(entries)
    packed = []
    # Prepare and validate all sheets before writing any outputs.
    for entry in entries:
        name = Path(entry['src']).name
        path = Path(sources) / name
        with Image.open(path) as source:
            if source.format != 'PNG' or source.mode != 'RGBA' or source.width != source.height or source.width < 4:
                raise ValueError(f'{path}: expected square RGBA PNG, at least 4x4')
            if source.getchannel('A').getextrema()[0] == 255:
                raise ValueError(f'{path}: source has no transparent background')
            sheet = Image.new('RGBA',(768,768),(0,0,0,0))
            for i in range(16):
                col,row = i % 4,i // 4
                box = (round(col*source.width/4),round(row*source.height/4),
                       round((col+1)*source.width/4),round((row+1)*source.height/4))
                cell = source.crop(box).resize((176,176),Image.Resampling.NEAREST)
                # Unmasked paste preserves RGBA bytes, including partial alpha.
                sheet.paste(cell,(col*192+8,row*192+8))
            validate_image(sheet,name)
            packed.append((name,sheet))
    out, runtime = Path(out),Path(runtime)
    out.mkdir(parents=True,exist_ok=True)
    runtime.parent.mkdir(parents=True,exist_ok=True)
    for name,sheet in packed:
        sheet.save(out/name,format='PNG',optimize=False,compress_level=9)
    runtime.write_text('const FIRE_FX_DATA='+json.dumps(entries,ensure_ascii=False,separators=(',',':'))+';\n',encoding='utf-8',newline='\n')


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--manifest',type=Path,default=DEFAULT_MANIFEST)
    parser.add_argument('--sources',type=Path,default=ROOT/'tools/fire_fx/sources')
    parser.add_argument('--out',type=Path,default=ROOT/'wwwroot/img/fx/fire')
    parser.add_argument('--runtime',type=Path,default=ROOT/'wwwroot/js/fire-fx-data.js')
    args = parser.parse_args()
    try:
        package(json.loads(args.manifest.read_text(encoding='utf-8')),args.sources,args.out,args.runtime)
    except (ValueError,OSError) as exc:
        parser.exit(1,f'Packaging failed: {exc}\n')
    print(f'Packed 9 sheets into {args.out}; runtime metadata: {args.runtime}')

if __name__ == '__main__':
    main()
