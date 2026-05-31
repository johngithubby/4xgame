# Hero Scripts

## Files

- `HeroCatalog.cs`: Defines the tiny Phase 3 in-code hero catalog used until ScriptableObject assets are worthwhile.
- `HeroDefinition.cs`: Stores hero id, display name, rarity, and minigame stat bonuses.
- `HeroInventory.cs`: Grants, lists ownership, equips, and reads equipped hero stat bonuses from local save data.
- `HeroRarity.cs`: Lists supported rarity labels for hero definitions.
- `HeroRewardSystem.cs`: Grants the first gameplay-earned hero from the minigame win reward path.

## Compile

Unity compiles these files as part of the `LaneSurvivor.Runtime` assembly.

## Behavior

Heroes are local-only in Phase 3. The first hero is earned through gameplay, auto-equipped, saved locally, listed on Base, manually equip-capable from the Base panel, and applied to minigame starting squad size and damage.
