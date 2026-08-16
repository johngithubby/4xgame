# EditMode Tests

## Files

- `LaneSurvivor.Tests.EditMode.asmdef`: Declares the EditMode test assembly so Unity Test Runner can discover these tests from the command line.
- `PhaseOneGameplayTests.cs`: Verifies gate modifiers, lane selection and shooting rules, zombie combat, mission data, generated geometry and materials, the imported SWAT leader and disabled legacy leader decal, explicit suit albedo/normal-map binding, Animator/controller setup, locomotion isolation, hidden unreliable weapon skins, pivot-local rigid weapon renderers, visible imported-barrel muzzle registration and exact live stock-to-muzzle target alignment, retained wing-survivor motion, generated survivor aim/recoil, imported female SWAT zombies with infected PBR materials, a shared tattered cutout shader and mesh, high-saturation per-spawn colour/property blocks, a complete nine-entry dominant palette cycle, guaranteed large tear regions, deterministic appearance signatures, varied missing tactical gear without global random-state consumption, hidden firearms and fallback primitives, enlarged bloody eyes, blood trails, drool and wounds, armor variants, deterministic asymmetric stumble motion and sampled support-foot grounding, visible actor bounds, camera framing, combat feedback, and responsive portrait layout constants.
- `SwatSurvivorAnimationImportTests.cs`: Verifies the animation-only Mixamo FBXs import as uncompressed root-locked Humanoids, preserves the authored run loop without loop-pose redistribution, and confirms that one unmasked base layer references the exact full-body idle/run motions.
- `PhaseThreeHeroTests.cs`: Verifies gameplay hero rewards, hero XP/level progression, manual coin level-up, manual hero equip state, hero stat bonuses, save persistence, and invalid equipped hero repair.
- `PhaseTwoProgressionTests.cs`: Verifies local coin collection, minigame win rewards, local daily objective progress/claim/rollover, HQ upgrade costs, shared requested duration curve, timers, progress, completion, and invalid timer repair, bio-lab/hangar/training/living-quarters upgrade costs, requested duration curve, progress, completion, and timer repair, mission normalization/selection/completion/unlocks through mission 8, mission status/reward hints, minigame bonuses, save reset, and save/load persistence.

## Compile

Unity compiles and runs these tests through the `LaneSurvivor.Tests.EditMode` Unity Test Framework assembly.

## Behavior

The tests focus on small deterministic gameplay and progression rules that do not require entering Play Mode.

For scene-level flow checks, see `Assets/Tests/PlayMode`.
