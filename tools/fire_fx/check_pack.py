"""Fixture-only checks; never touches production assets."""
import copy
import json
from pathlib import Path
import tempfile
import unittest
from PIL import Image
import pack
import validate

MANIFEST = Path(__file__).with_name('manifest.json')

class PackagingTests(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory()
        self.addCleanup(self.temp.cleanup)
        self.root = Path(self.temp.name)
        self.sources = self.root / 'sources'
        self.sources.mkdir()
        self.out = self.root / 'out'
        self.entries = json.loads(MANIFEST.read_text(encoding='utf-8'))
        for entry in self.entries:
            im = Image.new('RGBA', (1254, 1254))
            for i in range(16):
                box = (round(i % 4 * 1254 / 4), round(i // 4 * 1254 / 4),
                       round((i % 4 + 1) * 1254 / 4), round((i // 4 + 1) * 1254 / 4))
                im.paste((i * 15, 80, 200, 180), box)
            im.save(self.sources / Path(entry['src']).name)

    def build(self):
        pack.package(self.entries, self.sources, self.out, self.root / 'runtime.js')

    def test_pack_pixels_runtime_and_determinism(self):
        self.build()
        validate.validate_assets(self.entries, self.out)
        before = {p.name: p.read_bytes() for p in self.out.iterdir()}
        for entry in self.entries:
            with Image.open(self.out / Path(entry['src']).name) as im:
                self.assertEqual(im.size, (768, 768))
                self.assertEqual(im.mode, 'RGBA')
                for i in range(16):
                    x, y = i % 4 * 192, i // 4 * 192
                    self.assertEqual(im.getpixel((x+8, y+8)), (i*15,80,200,180))
                    self.assertEqual(im.getpixel((x+183,y+183)), (i*15,80,200,180))
                    self.assertEqual(im.getpixel((x+7,y+8))[3], 0)
        runtime = (self.root / 'runtime.js').read_text()
        self.assertEqual(json.loads(runtime.removeprefix('const FIRE_FX_DATA=').strip().removesuffix(';')), self.entries)
        self.build()
        self.assertEqual(before, {p.name:p.read_bytes() for p in self.out.iterdir()})

    def test_bad_metadata(self):
        for mutate in (
            lambda e: e.pop(), lambda e: e.append(e[0]),
            lambda e: e[0].update(anchorX=192), lambda e: e[0].update(anchorY=-1),
            lambda e: e[0].update(src='../evil.png'),
            lambda e: e[0].update(phases=[[0,.25,0,16]]),
            lambda e: e[0].update(motion='wrong'),
        ):
            entries = copy.deepcopy(self.entries)
            mutate(entries)
            with self.subTest(entries=entries[0]):
                with self.assertRaises(ValueError):
                    validate.validate_manifest(entries)

    def test_invalid_images(self):
        self.build()
        path = self.out / Path(self.entries[0]['src']).name
        for mode, size, color in [('RGBA',(767,768),(0,0,0,0)),
                                   ('RGBA',(768,768),(20,30,40,255)),
                                   ('RGB',(768,768),(20,30,40)),
                                   ('RGBA',(768,768),(0,0,0,0))]:
            Image.new(mode,size,color).save(path)
            with self.subTest(mode=mode,size=size,color=color):
                with self.assertRaises(ValueError):
                    validate.validate_assets(self.entries,self.out)
        self.build()
        with Image.open(path) as im:
            im.putpixel((7,20),(1,2,3,1))
            im.save(path)
        with self.assertRaises(ValueError):
            validate.validate_assets(self.entries,self.out)
        Image.new('RGBA',(768,768)).save(path,format='TIFF')
        with self.assertRaises(ValueError):
            validate.validate_assets(self.entries,self.out)
        path.unlink()
        with self.assertRaises(ValueError):
            validate.validate_assets(self.entries,self.out)

    def test_reject_invalid_sources_before_outputs(self):
        path = self.sources / Path(self.entries[-1]['src']).name
        for mode,size in [('RGBA',(1254,1253)),('RGB',(1254,1254)),('RGBA',(3,3))]:
            Image.new(mode,size).save(path)
            with self.assertRaises(ValueError):
                self.build()
            self.assertFalse(self.out.exists())
        Image.new('RGBA',(1254,1254),(20,30,40,255)).save(path)
        with self.assertRaisesRegex(ValueError,'transparent background'):
            self.build()
        self.assertFalse(self.out.exists())

if __name__ == '__main__':
    unittest.main(verbosity=2)
