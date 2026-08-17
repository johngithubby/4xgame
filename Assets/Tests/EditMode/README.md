# EditMode Tests

## Files

- `LaneSurvivor.Tests.EditMode.asmdef`: Declares the EditMode test assembly so Unity Test Runner can discover these tests from the command line.
- `PhaseOneGameplayTests.cs`: Verifies gate/shooting rules, zombie combat, mission data, generated geometry/materials, all three imported SWAT survivors, shared PBR assets, distinct dark wardrobe property blocks and gear masks, disabled legacy cards, three Animator bridges with one-third-cycle phase offsets, procedural/imported-bone isolation, hidden source weapon skins, rigid visible rifles, leader-left-right barrel rotation and exact target alignment, imported female SWAT zombies, their shared tattered shader/mesh, varied vivid palettes/holes/gear, infected details, deterministic stumble motion, visible bounds, camera framing, combat feedback, and portrait layout constants.
- `SwatSurvivorAnimationImportTests.cs`: Verifies the animation-only Mixamo FBXs import as uncompressed root-locked Humanoids, preserves the authored run loop without loop-pose redistribution, and confirms that one unmasked base layer references the exact full-body idle/run motions.
- `PhaseThreeHeroTests.cs`: Verifies gameplay hero rewards, hero XP/level progression, manual coin level-up, manual hero equip state, hero stat bonuses, save persistence, and invalid equipped hero repair.
- `PhaseTwoProgressionTests.cs`: Verifies local coin collection, minigame win rewards, local daily objective progress/claim/rollover, HQ upgrade costs, shared requested duration curve, timers, progress, completion, and invalid timer repair, bio-lab/hangar/training/living-quarters upgrade costs, requested duration curve, progress, completion, and timer repair, mission normalization/selection/completion/unlocks through mission 8, mission status/reward hints, minigame bonuses, save reset, and save/load persistence.

## Compile

Unity compiles and runs these tests through the `LaneSurvivor.Tests.EditMode` Unity Test Framework assembly.

## Behavior

The tests focus on small deterministic gameplay and progression rules that do not require entering Play Mode.

For scene-level flow checks, see `Assets/Tests/PlayMode`.
