# Hero Scripts

## Files

- `HeroCatalog.cs`: Defines the tiny Phase 3 in-code hero catalog used until ScriptableObject assets are worthwhile.
- `HeroDefinition.cs`: Stores hero id, display name, rarity, and minigame stat bonuses.
- `HeroInventory.cs`: Grants, lists ownership, equips, and reads equipped hero stat bonuses from local save data.
- `HeroRarity.cs`: Lists supported rarity labels for hero definitions.
- `HeroRewardSystem.cs`: Grants gameplay-earned heroes from the minigame win reward path and HQ level milestones.

## Compile

Unity compiles these files as part of the `LaneSurvivor.Runtime` assembly.

## Behavior

Heroes are local-only in Phase 3. `Mira Vanguard` is earned from the first minigame win, `Dax Medic` is earned when HQ reaches level 2, owned heroes are saved locally, the Base panel can cycle equipment, and the equipped hero applies minigame starting squad size and damage bonuses.
