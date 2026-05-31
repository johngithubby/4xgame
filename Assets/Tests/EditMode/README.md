# EditMode Tests

## Files

- `LaneSurvivor.Tests.EditMode.asmdef`: Declares the EditMode test assembly so Unity Test Runner can discover these tests from the command line.
- `PhaseOneGameplayTests.cs`: Verifies gate modifiers, lane selection rules, zombie defeat, and basic win/loss state evaluation.
- `PhaseTwoProgressionTests.cs`: Verifies local coin collection, minigame win rewards, HQ upgrade costs and timers, HQ completion, invalid timer repair, minigame bonuses, save reset, and save/load persistence.

## Compile

Unity compiles and runs these tests through the `LaneSurvivor.Tests.EditMode` Unity Test Framework assembly.

## Behavior

The tests focus on small deterministic gameplay and progression rules that do not require entering Play Mode.
