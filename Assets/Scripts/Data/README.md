# Data Scripts

## Files

- `GateModifierType.cs`: Defines the available gate operations for squad count and damage changes.
- `LevelDefinition.cs`: Stores the tunable Phase 1 level data, including squad defaults, gate placements, zombie placements, and pacing values.

## Compile

Unity compiles these files automatically as part of the main runtime assembly.

## Behavior

The minigame reads `LevelDefinition` assets to spawn a small local-only level without hard-coding every gate and zombie in the scene.
