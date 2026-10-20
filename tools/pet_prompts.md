# Linh thú — production art manifest

Created with built-in imagegen, then deterministic cropping/resizing with Pillow. Alpha is preserved. No generated UI screenshots are used as playable screens.

## Creature prompt set

Four square transparent atlases, each exactly three rows by three columns. Each row is one species; columns are baby, mature, divine. All creatures face right with expressive chibi eyes, detailed dark outlines, soft upper-left lighting, generous cell padding, no labels/grid/background/ground aura. Evolution preserves the palette/anatomy and adds tails, horns or plumage. Friendly companions distinct from hostile monsters.

- `pet_art/pets-design-1.png`: fire fox, ice rabbit, leafy deer.
- `pet_art/pets-design-2.png`: gold tortoise, violet storm eagle, turquoise water dragon.
- `pet_art/pets-design-3.png`: jade crane, lightning cat, vermilion phoenix.
- `pet_art/pets-design-4.png`: sacred dark-turquoise turtle with serpent tail, emerald dragon, white tiger.

`cut_pets.py` detects each connected creature rather than cutting rigid grid boundaries, then exports 320×221, 240×240 HD, 48×48, 24×24 RGBA to `wwwroot/img/pet/`. Source atlases stay in the project for reproducibility.

## Item prompt

One transparent 3×3 icon atlas: ivory, jade, cyan, azure-violet, purple/gold spirit eggs; green pet-food pill in bronze dish; purple transformation pill; red beast-blood vial; gold paw medallion. Fine dark outlines, centered isolated objects, no text. `cut_pet_items.py` exports 64px RGBA.

## Scenery prompt

Landscape nighttime blue-black karst peaks, silver moon, old pagodas with small amber lanterns, fog and waterfalls. Broad empty stone courtyard at 80% height for existing side-view combat. No UI/text/characters/pets. Saved as `wwwroot/img/backgrounds/concept-courtyard-v1.webp`. Existing HUD coordinates retained; scene painting renders below existing sprites at display resolution.
