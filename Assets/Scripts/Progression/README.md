# Progression Scripts

## Files

- `PlayerProgression.cs`: Defines progression rules for collecting coins, claiming one minigame win reward per run, starting HQ upgrades, completing HQ timers, selecting unlocked missions, unlocking the next mission after frontier wins, and converting HQ level into minigame bonuses.
- `UpgradeTimer.cs`: Calculates persistent UTC-based upgrade timer state and clears invalid persisted timer values before they can crash scene startup.

## Compile

Unity compiles these files as part of the `LaneSurvivor.Runtime` assembly.

## Behavior

Progression stays local and deterministic. Minigame wins grant a fixed local coin reward, mission frontier wins unlock the next authored local mission up to mission 4, hero rewards are handled by the hero system, and upgrade timers use UTC ticks from the save file so closing and reopening the game preserves countdown progress, with range checks for corrupted local saves.
