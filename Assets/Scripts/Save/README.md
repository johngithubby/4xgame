# Save Scripts

## Files

- `SaveGameData.cs`: Stores local prototype progress such as coins, HQ level, active HQ upgrade timer, selected mission, highest unlocked mission, completed mission levels, legacy unlocked minigame level, owned heroes, equipped hero, per-hero level/XP, and local daily objective state; it also normalizes corrupted timer, mission, hero, and daily objective data after load.
- `SaveGameManager.cs`: Loads, saves, and resets `SaveGameData` as local JSON in Unity's persistent data path, with a test-only custom path override.

## Compile

Unity compiles these files as part of the `LaneSurvivor.Runtime` assembly.

## Behavior

The save system is intentionally local-only. It repairs malformed or impossible local data to keep prototype scene startup resilient, migrates the older `unlockedMinigameLevel` field into the current mission fields, backfills completed predecessor missions from sequential unlock progress, tracks the current UTC-day objective locally, and includes a development reset helper, but it does not use accounts, cloud save, networking, or server validation in early phases.
