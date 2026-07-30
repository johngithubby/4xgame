# EditMode Tests

## Files

- `LaneSurvivor.Tests.EditMode.asmdef`: Declares the EditMode test assembly so Unity Test Runner can discover these tests from the command line.
- `PhaseOneGameplayTests.cs`: Verifies gate modifiers, lane selection and shooting rules, zombie combat, mission data, generated geometry and materials, the imported SWAT leader and disabled legacy leader decal, explicit suit albedo/normal-map binding, Animator/controller setup, procedural isolation from imported bones, retained wing-survivor motion, target-aware gameplay-root aim and hidden recoil, connected zombie limbs, visible actor bounds, camera framing, combat feedback, and responsive portrait layout constants.
- `PhaseThreeHeroTests.cs`: Verifies gameplay hero rewards, hero XP/level progression, manual coin level-up, manual hero equip state, hero stat bonuses, save persistence, and invalid equipped hero repair.
- `PhaseTwoProgressionTests.cs`: Verifies local coin collection, minigame win rewards, local daily objective progress/claim/rollover, HQ upgrade costs, shared requested duration curve, timers, progress, completion, and invalid timer repair, bio-lab/hangar/training/living-quarters upgrade costs, requested duration curve, progress, completion, and timer repair, mission normalization/selection/completion/unlocks through mission 8, mission status/reward hints, minigame bonuses, save reset, and save/load persistence.

## Compile

Unity compiles and runs these tests through the `LaneSurvivor.Tests.EditMode` Unity Test Framework assembly.

## Behavior

The tests focus on small deterministic gameplay and progression rules that do not require entering Play Mode.

For scene-level flow checks, see `Assets/Tests/PlayMode`.
