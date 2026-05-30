# EditMode Tests

## Files

- `LaneSurvivor.Tests.EditMode.asmdef`: Declares the EditMode test assembly so Unity Test Runner can discover these tests from the command line.
- `PhaseOneGameplayTests.cs`: Verifies gate modifiers, lane selection rules, zombie defeat, and basic win/loss state evaluation.

## Compile

Unity compiles and runs these tests through the `LaneSurvivor.Tests.EditMode` Unity Test Framework assembly.

## Behavior

The tests focus on small deterministic gameplay rules that do not require entering Play Mode.
