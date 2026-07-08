# Progression & Balance Proposal — Response

> Written against the codebase as of 2026-07-08 (commit `08ed7bc`, "Save/Load"). Responds to the `Progression Proposal (Draft)` doc.

## Summary

Most of the proposal's *shape* already matches what's implemented: a single-formula XP curve, per-level skill-point gating, and archetype-only enemies with no level concept are all real, working systems today — not things that needed to be built from scratch. The gaps were narrower than they looked from the outside: a few numbers were unset or left at prototype placeholders, one item (`Helmet`) was quietly unequippable and unsellable due to an enum-reordering bug unrelated to this proposal, and there was only one quest and two shop items in total — not enough content to carry a 10-level arc regardless of how the curve was tuned.

This pass retunes the numbers, fixes the Helmet bug, and adds two new quests and three new items so there's an actual Level 1–10 arc to walk through, not just a formula.

## What was already true vs. what the proposal assumed

| Proposal assumption | Reality in code | Verdict |
|---|---|---|
| No enemy levels, archetype-only stats | `EnemyArchetypeSO` has no level field — purely maxHealth/armor/attackPower/crit/expReward | Already matches, no change |
| L1 Auto Attack → L2 Power Strike → L5 cleave, later levels upgrade existing skills | `SkillLevelData` on Power Strike (`requiredPlayerLevel: 2`) and Stomp (`5 / 7 / 9`, escalating `damageMultiplier`) already encodes this | Already matched **in data**, but `Villager.asset.learnableSkills` was empty on disk (the field was added to `ClassDefinitionSO` after this asset was last saved) — neither skill was learnable at any level. Fixed by populating the list. |
| Combat-focused kill quests, ~35–45% of XP from quests | Only one quest existed (`KillGoblins`), targeting a single enemy archetype; `requiredAmount: 1` contradicted its own description text ("kill 10 of them"); `xpReward: 200` against a curve that only needed ~208 total XP to reach level 10 | One quest structurally cannot deliver a 35–45% share across 10 levels. Retuned the existing quest and added two more (see below). |
| Enemies drop Gold and Experience | `Enemy_Health.OnMonsterDefeated` only ever carried `(exp, archetype)`. The only `GoldManager.AddGold` call site in the whole codebase was the shop's Sell transaction. | Confirmed gap — **not implemented**, and deliberately **not built this pass** per your call. Gold income now comes from quests instead (see "Code change" below), which is smaller and more contained than hooking every enemy death. |
| Equipment initially purchased from vendors | `Milena` (the only shop) listed only Sword and Potion. `Adaga` and `Helmet` existed as data but weren't purchasable anywhere, and 6 of 8 equip slots (Body/Legs/Feet/OffHand/Necklace/Ring) had zero items in the entire project. | Filled 3 of those empty slots with new items, added them plus Adaga/Helmet to Milena's shop. |
| ~2 hour target for Level 1–10 | `ExpManager.ExpToLevel = round(baseExpToLevel * expGrowthMultiplier ^ (level-1))`, at `baseExpToLevel=10, expGrowthMultiplier=1.2` — **total XP to reach level 10 was only ~208** (a Goblin was worth 2 XP, the one quest was worth 200 XP by itself) | Curve was far too shallow for a 2-hour arc — a single quest turn-in nearly maxed it. Retuned (below). |

## Answers to the 7 questions

**1. Does this progression fit the existing architecture?**
Yes, more than the draft assumed — the level-gated skill system and archetype-only enemies were already built close to spec. The real gaps were content volume (one quest, two shop items) and a couple of unwired/incorrect values, not architecture.

**2. Would you change the XP curve?**
Kept the existing single-formula design in `ExpManager` rather than switching to an authored table (it already does what the proposal's table was trying to do, with two tunable numbers instead of ten rows to keep in sync). Retuned the constants:

- `baseExpToLevel`: 10 → **25**
- `expGrowthMultiplier`: 1.2 → **1.25**

New per-level thresholds: 25, 31, 39, 49, 61, 76, 95, 119, 149 XP — cumulative **≈644 XP** to reach level 10, versus ≈208 before.

**3. Would another quest distribution work better?**
The 35–45%-from-quests target can't be hit meaningfully with a single quest asset — it only becomes a real ratio once there's more than one. Added two more quests (Orc Patrol, Ranged Threat) alongside a retuned Goblin Trouble, bringing total quest XP to 270 — **≈42% of the 644-XP curve**, inside your target range.

**4. Is the expected pacing (2 hours) realistic?**
Plausible given the retuned curve, the enemy XP values below, and the game's real-time combat pace (Auto Attack every 1.5s, enemy packs via the flocking system) combined with the existing configurable `EnemySpawner` respawn interval — but this is arithmetic, not a playtested number. Recommend an actual playtest pass once these values are live rather than trusting the math alone.

**5. Would you change the skill unlock progression?**
No — the level gates (2, and 5/7/9) already match your plan almost exactly, including "later levels upgrade the existing skill instead of granting a new one" (Stomp's own 5/7/9 multiplier scaling already does this). The only fix needed was wiring: `Villager.learnableSkills` was empty, so neither skill was reachable regardless of player level. Fixed.

**6. Is removing enemy levels a good long-term decision?**
Yes — it's already how the codebase works (`EnemyArchetypeSO` has no level field at all), and it fits an archetype-driven roster well. Nothing to change.

**7. Are there existing systems that suggest a different approach?**
The shop/gold economy already naturally enforces "no early gear drops" — there simply weren't any combat drops to begin with. But that also meant gold had no income source beyond the 100 starting gold and selling items back. Rather than hook every enemy death (which you asked to leave out this pass), quest gold rewards were added instead — a smaller, self-contained change that mirrors the existing `xpReward` pattern exactly. Worth deciding later whether enemy gold drops become the primary income source once there's more combat content; for now quests carry it.

## New content

### Items (fills 3 of 6 previously-empty equip slots)

| Item | Slot | Modifiers | Rarity | Buy price | Sell value |
|---|---|---|---|---|---|
| Leather Tunic | Body | Armor +8, Max Health +15 | Common | 20g | 10 |
| Worn Boots | Feet | Move Speed +0.3, Armor +3 | Common | 15g | 8 |
| Bronze Ring | Ring | Strength +2, Critical Chance +2 | Uncommon | 25g | 13 |

Legs, Off Hand, and Necklace are still empty — left for a future content pass rather than filled with arbitrary items just to claim full coverage. No sprite art exists for the new items yet (`itemSprite` left unassigned) — needs an artist pass in the Inspector, same as the existing `ShopSetupTool`'s "assign the sprite later" placeholder convention.

### Quests (now three total, spanning the arc)

| Quest | Target | Amount | XP | Gold | Rough level band |
|---|---|---|---|---|---|
| Goblin Trouble (retuned) | Goblin | 10 (was 1) | 50 (was 200) | 20 (new) | ~1–3 |
| Orc Patrol (new) | Orc | 8 | 90 | 40 | ~4–6 |
| Ranged Threat (new) | Orc Ranged | 5 | 130 | 70 | ~7–9 |

Quest gold totals 130; combined with the unchanged 100 starting gold, that's 230g available against 98g of total gear cost across all seven shop items — enough to fully gear up with room left for potions, without making gold trivial from minute one.

**Not done, needs the Unity Editor:** placing NPC GameObjects for Orc Patrol / Ranged Threat in the scene, wiring `NPCInteractable.quest` to the new assets, and running `Tools > Quests > Setup Quest Giver Indicator`. Hand-editing scene YAML to create new TextMeshPro-bearing GameObjects is exactly what the project's own tooling comments warn against — it's an Editor-only step.

## Bug fixed along the way

`Helmet.asset` had `itemType: 2`. Under the current `ItemType` enum (`Consumable=0, Material=1, Quest=2, Head=3, ...`), that's `Quest`, not `Head` — so the Helmet could not be equipped (`EquippedSlot.CanEquip` does a raw enum comparison) and could not be sold (`ItemSO.IsSellable` excludes `Quest` items). The asset almost certainly predates `Quest` being inserted into the enum — the same class of bug the project's `QuestState` int→string save migration exists to prevent, just missed here since it's not save data. Fixed: `itemType: 2` → `3`.

## Retuned enemy XP rewards

| Enemy | Before | After |
|---|---|---|
| Goblin | 2 | 4 |
| Orc | 3 | 7 |
| Orc Ranged | 3 | 9 |

## Verification checklist

- [ ] Open `KillGoblins`, `OrcPatrol`, `RangedThreat`, `Goblin`, `Orc`, `Orc Ranged`, `Villager`, `Milena`, `Potion`, `Adaga`, `Helmet`, `Leather Tunic`, `Worn Boots`, `Bronze Ring` in the Inspector — confirm values match this doc (catches any hand-edited YAML mistake).
- [ ] Select "XP Canvas" in `SampleScene.unity` — confirm `Exp Manager` shows `Base Exp To Level = 25`, `Exp Growth Multiplier = 1.25`.
- [ ] Select the `QuestManager` GameObject — confirm `All Quests` lists 3 entries.
- [ ] Play Mode: confirm Helmet can be equipped in the Head slot and sold to Milena.
- [ ] Play Mode: confirm Power Strike appears in the Skill Book at level 2, Stomp at level 5 (with points available).
- [ ] Play Mode: confirm Milena lists 7 purchasable items at the prices above.
- [ ] Play Mode: kill 10 Goblins and turn in Goblin Trouble — confirm +50 XP and +20 gold.
