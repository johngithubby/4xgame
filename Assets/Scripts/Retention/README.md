# Retention Scripts

## Files

- `DailyObjectiveProgression.cs`: Tracks a local UTC-day objective that counts minigame wins, rolls over on a new UTC day, exposes compact Base HUD status text, and pays a fixed local coin reward when the player claims the completed objective.

## Compile

Unity compiles these files as part of the `LaneSurvivor.Runtime` assembly.

## Behavior

The retention loop is local-only. It does not use accounts, networking, ads, purchases, or a server clock. A minigame win advances the current UTC day's objective up to the required count, the Base screen can claim the reward once after completion, and the next UTC day starts a fresh objective from the saved day number.
