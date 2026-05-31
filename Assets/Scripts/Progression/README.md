# Progression Scripts

## Files

- `PlayerProgression.cs`: Defines Phase 2 progression rules for collecting coins, claiming one minigame win reward per run, starting HQ upgrades, completing HQ timers, and converting HQ level into minigame bonuses.
- `UpgradeTimer.cs`: Calculates persistent UTC-based upgrade timer state and clears invalid persisted timer values before they can crash scene startup.

## Compile

Unity compiles these files as part of the `LaneSurvivor.Runtime` assembly.

## Behavior

Progression stays local and deterministic. Minigame wins grant a fixed local coin reward, and upgrade timers use UTC ticks from the save file so closing and reopening the game preserves countdown progress, with range checks for corrupted local saves.
