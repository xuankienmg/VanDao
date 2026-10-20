# Linh Thú server contract and balance

All JSON property names use the application's existing camelCase policy. No client assets are generated here.

## Contract

`GET /api/data` adds `petDefs: PetDef[]` and `petSpinWeights: [45,30,17,7,1]`. Each definition has `id, name, element, rarity, role, desc, forms, talentText, skillText, skillEvery, talent, talentValue, skill, atk, def, hp`. Each of its three forms has `name, image, shareMult, skillName`. Images are `img/pet/<id>_<0|1|2>.png`. Elements use the existing Vietnamese IDs: hoa, bang, moc, tho, loi, thuy, gio (Phong). Rarities use common/uncommon/rare/epic/legendary.

The current player state adds `pet: { owned: PetDto[], hatch: HatchDto[], capacity, levelCap, guardSlots, hatchSlots }`. It is populated even with no pets. PetDto has exactly the requested fields: `id, defId, name, element, rarity, level, exp, expNeed, form, formName, slot, atk, def, hp, aptAtk, aptDef, aptHp, shareAtk, shareDef, shareHp, talentText, skillText, skillEvery, evolveCost, canEvolve, image`. Costs use `{item, need, have}`. Share fields are actual flat contributions from the currently assigned slot, zero for resting pets; talents are described separately.

HatchDto: `slot, egg, readyAt` (UTC ISO timestamp), `readyInSeconds, ready, instantCost`. Empty hatch slots are omitted. Use hatchSlots to draw all available slots.

BattleDto adds nullable `pet: { id, name, image, element, casts: [{turn, dmg, heal, guard, skill}] }`. Turn is zero-based in the sampled battle timeline; skill is the display skill name. Multiple casts may map to one sampled turn. Every actual cast maps to the first sampled turn at or after it so it is not silently discarded. Damage/healing/shields are applied in the server simulation. Healing reports actual restored HP.

ChestOddsDto additionally has `pets: [{item,pct}]`, including eggs and pet materials/food. These odds come from the same distribution used for opening chests, including chapter and difficulty upgrades.

All actions use `/api/players/{id}` and return the existing ActionResponse with updated state:

- POST `/pet/{petId}/feed?n=1|10|0`, where zero means max. Invalid quantities rejected; batches stop at available food or level cap.
- POST `/pet/{petId}/evolve`
- POST `/pet/{petId}/slot/{slot}`: 0 resting, 1 active, 2 and 3 guards. Assigning an occupied slot rests its prior occupant.
- POST `/pet/{petId}/release`: removes the pet and refunds floor(foodSpent/2). No client-provided refund or confirmation flag is trusted.
- Existing POST `/use/{eggId}` also starts incubation in the first free unlocked slot (one egg per use); occupied incubators reject without consuming an egg.
- POST `/hatch/{eggId}?slot=0`, `/hatch/now?slot=0`, `/hatch/collect?slot=0`.
- POST `/spin?mode=pet&n=1..10`. Existing spin mode remains unchanged.

## Balance and costs

Realm indices are zero-based. Level caps are 20/40/60/80/90/100, remaining realms retain 100. Capacity is 20 + 5 per realm. One active slot; guard slots unlock at realms 2 and 4. Hatch slot 2 unlocks at realm 2. Incubating eggs reserve capacity. One egg consumed at start; collection is explicit and works after offline time using persisted UTC deadlines.

Egg IDs, from lowest to highest: thu_dan_pham, thu_dan_linh, thu_dan_huyen, thu_dan_dia, thu_dan_thien. Incubation times: 10 minutes, 30 minutes, 1 hour, 4 hours, 8 hours. Instant hatch costs 100 stones per started remaining minute, zero once ready.

Aptitudes independently roll 60–100 inclusive, or 80–100 for Heavenly eggs. Common eggs use the two entry species (fox/rabbit); uncommon eggs also use those species, rare eggs deer/turtle/eagle, epic eggs dragon/crane/cat, legendary eggs the four divine beasts. Species rarity remains fixed; there are twelve species and no invented thirteenth common species.

Each food gives 100 EXP. Next-level EXP is 50 + 25*currentLevel. Consumption stops immediately on the realm cap and excess EXP at that cap is discarded. Refunds count only actual food consumed. Dev-tainted characters have a level cap of 100; release builds always enforce realm caps.

Form 0 -> 1 requires level 30, 10 hoa_hinh_dan, 2 kim_o_tinh_hach and 10,000 stones. Form 1 -> 2 requires level 60, realm >= 3, 20 hoa_hinh_dan, 10 than_thu_huyet, 3 huyet_giao_hach and 100,000 stones.

Brews produce one item:
- linh_thu_dan: 2 tay_tuy_thao + 100 stones.
- hoa_hinh_dan: 3 thien_nien_sam + 1,000 stones.
- than_thu_huyet: 5 cuu_diep_hon_lien + 5,000 stones.

Pet spin costs one existing ticket per reward, with weights 45/30/17/7/1. Results share existing spin history. Eggs/food/evolution materials are excluded from the market even with the dev ticket exception enabled.

Boss chests have egg chances by chapter 1–10: 15/15/12/10/10/10/8/8/8/8 percent. Egg grade is Linh in chapters 1–2, Huyen in chapter 3, Dia in chapters 4–6, Thien thereafter. Hard upgrades one grade with 50% chance; nightmare always upgrades one, capped at Thien. Boss chests also give evolution material: normal 4.5% hoa_hinh_dan; hard/nightmare 3% hoa_hinh_dan and 1.5% than_thu_huyet. Normal and rare chests have 5% food chance, yielding 1 or 3 food respectively. The realm-gated enlightenment probability stays exactly cd.Enlightenment; other old rewards are scaled by (1 - enlightenment - petChance) / (1 - enlightenment) to leave a normalized one-reward distribution; opening still consumes exactly one existing production random roll.

Base pet Atk/Def/Hp is round(24/12/120 times the rarity scale), with rarity scales .8/1/1.2/1.4/1.6 from Pham to Thien. Each final stat is round(base * (1 + (effectiveLevel-1)*0.22*aptitude/100) * (1 + form*0.4) * realmScale * talentBudget). Realm scales for indices 0–6 are .36/.385/1.47/2.98/13.3/59.8/299. Thanh Long uses a .74 flat-stat budget to account for its existing +10% owner HP talent; all other species use 1. These are explicit static curves; no runtime owner gear or power feedback is used. Active sharing is 10/15/22%; guards receive 30% of that, rounded down per stat per pet. Active talents apply fully; guards apply 30%. Resting pets give nothing.

All twelve talents from the plan are active: crit, reduction, regen, defense %, extra-hit chance, cultivation %, speed, penetration, phoenix bonus skill damage (3% monster HP per cast), initial shield, HP %, critical damage %. Existing combat caps apply to capped effects. Skills trigger every 5 turns for Linh, 4 for Huyen/Dia and 3 for Thien. Damage casts deal 60/90/120% owner Atk with a separate deterministic 90–110% roll, subtract monster defense and respect monster damage reduction. Heals restore 6/9/12% max HP; guard casts grant 8/12/16% max HP.

## Persistence and verification

Pets persist in a cascade-owned Pets table; Player.Hatch stores `slot|eggId|readyUnix` entries separated by semicolons. FoodSpent is persisted for safe refunds. Legacy upgrade creates the table/index and adds Hatch without resetting saves. Both reset flows clear pets and hatch. Dev admin give supports every new item; `petmax` raises owned pets to level 100/form 2 without spending materials.

Player action handlers serialize load/mutate/save within the local server to prevent overlapping collection and release requests. Existing combat RNG constructors and their draws are unchanged; pet casts use their own trip-seeded RNG.

Run focused checks with `dotnet run --project tools/pet_checks/pet_checks.csproj --no-launch-profile`. The harness uses a temporary SQLite DB, simulates a pre-pet schema, and checks upgrades, persistence, offline hatch, gates, refunds, crafting sources, normalized probabilities and deterministic combat. It never touches the player's real save.

Desktop build while the game is running: `dotnet build --no-restore -p:OutputPath=tools/pet_checks/desktop-build/`.

The 45 no-pet golden battle hashes were generated by running the original HEAD production GameService simulator against NoPetRegression.Cases, then comparing the full serialized BattleDto to the new simulator. Coverage includes realms 0/1/2/4/6, no gear and qualities 4/9, learned skills, ordinary areas and bosses. All matched. The test retains immutable hashes instead of a duplicate production implementation.

PetBalance.Print reports peak-stage power with a full same-realm Huyen (quality 2) gear set, no learned skills or guards, aptitude 90 and each realm's maximum legal pet level/form. It measures the entry fox in all realms and all four Heavenly form-2 species from realm 3 onward. The fixture now asserts entry pets remain within 8–12% and Heavenly form-2 pets within 16–20%; it also checks gear independence and continued level growth.

Latest verification after balance tuning: 806 checks pass, including exact realm-gated enlightenment/proportional other-branch scaling and all 45 original no-pet combat golden hashes. All entry fixtures pass 8–12% power gain, and all legal Heavenly form-2 fixtures pass 16–20%, including Thanh Long. Exact current before/after values are in balance-results.txt. Power is the existing displayed power metric; it does not measure active heal/shield/skill damage or all talent combat value.
