# Data Scripts

## Files

- `GateModifierType.cs`: Defines the available gate operations for squad count, additive damage, and multiplicative damage changes.
- `LevelDefinition.cs`: Stores the tunable Phase 1 level data, including level number, squad defaults, lane positions, gate placements, zombie placements, zombie enemy types, and pacing values.
- `ZombieEnemyType.cs`: Defines the basic and armored zombie variants used by authored local missions.

## Compile

Unity compiles these files automatically as part of the main runtime assembly.

## Behavior

The minigame reads `LevelDefinition` assets to spawn small local-only levels without hard-coding every gate and zombie in the scene.
