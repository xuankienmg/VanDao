# Accepted fire sprite storyboards and packaging contract

The user approved the fire/gold demo and full implementation plan on 2026-10-19.
This pipeline owns packaging and metadata only. Artwork production and runtime
integration are separate work. The 7 active skill IDs remain unchanged; the 10
foundation/passive skills keep their existing static icons and subtle auras.

## Visual direction

Keep the accepted pixel-art fire/gold style: readable silhouettes, warm flame
edges, gold/white hot cores, true transparent backgrounds, consistent pixel
texture, and increasing complexity with realm. No characters, enemies, labels,
cell borders, baked checkerboards, or full-screen white flashes.

Every sheet reads left to right, top to bottom through 16 frames:
0-3 gather energy; 4-7 launch/materialize; 8-11 impact; 12-15 dissipate.
The runtime intervals are [0,.25), [.25,.45), [.45,.60), [.60,.75);
after .75 the effect is no longer drawn. Projectile art travels left to right.

- `hoa_cau`: small, medium, large fireballs with compact flame tails and a
  distinct impact burst. Three separate source/output sheets, scales 1/1.25/1.5.
- `liet_diem`: low ground flare expands into a ring around the target, scale 1.8.
- `hoa_long`: elongated fire dragon with a leading head, trailing body and
  afterimage, scale 1.65.
- `phan_thien`: overhead fire seal calls a vertical descending column, scale 2.
- `xich_diem_thien`: dark red dragon coils into a ring before a crimson burst,
  scale 2; preserve the accepted warm palette.
- `than_hoa`: elaborate white/gold seal, radiant core, runes and descending
  heavenly fire; keep the demo's lower impact anchor (96,166), scale 2.1.
- `hu_khong_hoa`: dark violet rift contracts inward, then bursts into violet/gold
  flame, scale 2; retain a distinct silhouette and warm gold highlights.

Projectile anchors are (96,128); other ground anchors are (96,148), except
`than_hoa` above. `hoa_cau` and `hoa_long` use projectile motion; all others
use ground motion. These are frame pixel coordinates, not screen coordinates.

## Packaging

`manifest.json` is the authoritative top-level array of nine entries. Sources
have the same basenames as `src`, including `hoa_cau_small.png`,
`hoa_cau_medium.png`, and `hoa_cau_large.png`. Other names match skill IDs.
Sources must be square RGBA PNGs containing a 4x4 grid. Sizes such as 1254px
are accepted: each boundary uses Python `round(index * dimension / 4)`.
Each crop is nearest-neighbor resized to 176x176, then copied without an alpha
mask into (8,8) of a transparent 192x192 frame. Output sheets are 768x768.
No chroma-key, synthesis, recoloring, retouching, alpha threshold, or trimming.
All sheets are prepared and validated before output writes begin.

The packer emits `const FIRE_FX_DATA=<manifest JSON>;` to the explicitly
selected runtime path. It does not edit the renderer or HTML. PNG encoding is
deterministic within the same Pillow/zlib environment; versions may change
compressed bytes without changing pixels.

Run from the repository root in PowerShell:

```powershell
$firePython = 'C:/Users/KienBui/.cache/codex-runtimes/codex-primary-runtime/dependencies/python/python.exe'
& $firePython -B tools/fire_fx/check_pack.py
& $firePython -B tools/fire_fx/pack.py --manifest tools/fire_fx/manifest.json --sources tools/fire_fx/sources --out wwwroot/img/fx/fire --runtime wwwroot/js/fire-fx-data.js
& $firePython -B tools/fire_fx/validate.py --manifest tools/fire_fx/manifest.json --out wwwroot/img/fx/fire
```

The validator requires all nine unique skill/variant references, exact frame
geometry, valid phases, finite positive scales, bounded anchors, RGBA PNGs,
nonempty frames and at least eight fully transparent pixels on every frame
edge. Errors return nonzero status. Visual review must still catch baked
backgrounds, labels, characters and poor animation continuity.

## Scoped implementation verification

Fixture tests use temporary images only: 1254px crop boundaries, all 144 packed
frames, partial alpha preservation, exact transparent padding, runtime JSON
round-trip, repeated PNG byte equality, malformed metadata, missing files,
wrong image dimensions/mode, opaque/empty images, padding violations and invalid
sources rejected before output writes. Production artwork is not packed or
modified by this implementation task. No commits are made.
