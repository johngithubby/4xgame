# Save Scripts

## Files

- `SaveGameData.cs`: Stores local prototype progress such as coins, HQ level, active HQ upgrade timer, and unlocked minigame level; it also normalizes corrupted timer data after load.
- `SaveGameManager.cs`: Loads, saves, and resets `SaveGameData` as local JSON in Unity's persistent data path, with a test-only custom path override.

## Compile

Unity compiles these files as part of the `LaneSurvivor.Runtime` assembly.

## Behavior

The save system is intentionally local-only. It repairs malformed or impossible local data to keep prototype scene startup resilient and includes a development reset helper, but it does not use accounts, cloud save, networking, or server validation in Phase 2.
