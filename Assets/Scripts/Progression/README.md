# Progression Scripts

## Files

- `PlayerProgression.cs`: Defines progression rules for collecting coins, claiming one minigame win reward per run, starting HQ upgrades, completing HQ timers, starting bio-lab upgrades with the V6-1 duration curve, completing bio-lab timers, selecting unlocked missions, tracking completed missions, labeling mission status/reward hints, unlocking the next mission after frontier wins up to mission 8, and converting HQ level into minigame bonuses.
- `UpgradeTimer.cs`: Calculates persistent UTC-based upgrade timer state and clears invalid persisted timer values before they can crash scene startup.

## Compile

Unity compiles these files as part of the `LaneSurvivor.Runtime` assembly.

## Behavior

Progression stays local and deterministic. Minigame wins grant a fixed local coin reward, mission frontier wins mark the cleared mission complete and unlock the next authored local mission up to mission 8, replays keep completed missions replayable for coins, hero rewards are handled by the hero system, HQ upgrades use a fixed local timer, and bio-lab upgrades use the requested 1s, 3s, 10s, 60s, then x5 duration curve. Upgrade timers use UTC ticks from the save file so closing and reopening the game preserves countdown progress, with range checks for corrupted local saves.
