# EditMode Tests

## Files

- `LaneSurvivor.Tests.EditMode.asmdef`: Declares the EditMode test assembly so Unity Test Runner can discover these tests from the command line.
- `PhaseOneGameplayTests.cs`: Verifies gate modifiers, damage multiplier gates, contact-time gate resolution, brief post-contact gate visibility, gate label material preservation, lane selection rules, automatic same-lane shooter targeting, generated muzzle-origin shot events plus target-aware visible weapon recoil, cross-lane shooter rejection, zombie defeat, armored zombie damage reduction, basic win/loss state evaluation, mission definition selection through mission 8, opaque tintable placeholder material creation, always-visible combat feedback material creation, collider-free cube/sphere/cylinder/vertical-card geometry creation, generated survivor anchor and zombie body hierarchies, visible zombie-sized 3D armored generated survivor bodies with legacy cutouts absent, distinct survivor rifle/shotgun/SMG weapon profiles with direct forward-facing muzzle anchors, eye-level and hip-fire weapon hold heights, stabilized survivor firing arms during running, target-facing survivor root/chest/head rotation, connected hip/knee/shin limb pivots, high-contrast zombie leg material visibility, target-facing shot yaw, survivor run and zombie shamble animation, flat road and finish surfaces, prototype visual height separation, optional compact screen-space player marker constants, required marker RectTransform parent validation, visible world player mesh rendering, inward side-lane spacing, disabled low gate footprint decals, restored depth-safe world-shot tracers and feedback labels, gate and humanoid zombie visual bounds, angled perspective camera framing constants, and responsive camera FOV updates for narrow portrait aspects.
- `PhaseThreeHeroTests.cs`: Verifies gameplay hero rewards, hero XP/level progression, manual coin level-up, manual hero equip state, hero stat bonuses, save persistence, and invalid equipped hero repair.
- `PhaseTwoProgressionTests.cs`: Verifies local coin collection, minigame win rewards, local daily objective progress/claim/rollover, HQ upgrade costs, shared requested duration curve, timers, progress, completion, and invalid timer repair, bio-lab/hangar/training/living-quarters upgrade costs, requested duration curve, progress, completion, and timer repair, mission normalization/selection/completion/unlocks through mission 8, mission status/reward hints, minigame bonuses, save reset, and save/load persistence.

## Compile

Unity compiles and runs these tests through the `LaneSurvivor.Tests.EditMode` Unity Test Framework assembly.

## Behavior

The tests focus on small deterministic gameplay and progression rules that do not require entering Play Mode.

For scene-level flow checks, see `Assets/Tests/PlayMode`.
