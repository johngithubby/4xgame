# Hero Scripts

## Files

- `HeroCatalog.cs`: Defines the tiny Phase 3 in-code hero catalog used until ScriptableObject assets are worthwhile.
- `HeroDefinition.cs`: Stores hero id, display name, rarity, and minigame stat bonuses.
- `HeroHudController.cs`: Displays the dedicated Hero screen, owned hero rows, equipment state, coin cost, and button states.
- `HeroInventory.cs`: Grants, lists ownership, equips, reads hero level/XP, and reads equipped hero stat bonuses from local save data.
- `HeroProgression.cs`: Awards equipped-hero XP from minigame wins, resolves level-ups, handles manual coin level-ups, and exposes the per-level damage bonus.
- `HeroRarity.cs`: Lists supported rarity labels for hero definitions.
- `HeroRewardSystem.cs`: Grants gameplay-earned heroes from the minigame win reward path and HQ level milestones.
- `HeroSceneBootstrap.cs`: Builds the local-only Hero scene at runtime and connects equip, level-up, and back navigation actions.

## Compile

Unity compiles these files as part of the `LaneSurvivor.Runtime` assembly.

## Behavior

Heroes are local-only in Phase 3. `Mira Vanguard` is earned from the first minigame win, `Dax Medic` is earned when HQ reaches level 2, owned heroes are saved locally, the Base panel and dedicated Hero scene can cycle equipment, minigame wins award equipped-hero XP, manual level-up spends local coins, and the equipped hero applies minigame starting squad size and level-scaled damage bonuses.
