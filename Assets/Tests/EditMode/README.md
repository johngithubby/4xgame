# EditMode Tests

## Files

- `LaneSurvivor.Tests.EditMode.asmdef`: Declares the EditMode test assembly so Unity Test Runner can discover these tests from the command line.
- `PhaseOneGameplayTests.cs`: Verifies gate modifiers, contact-time gate resolution, brief post-contact gate visibility, gate label material preservation, lane selection rules, automatic same-lane shooter targeting, cross-lane shooter rejection, zombie defeat, basic win/loss state evaluation, mission definition selection, opaque tintable placeholder material creation, collider-free placeholder geometry creation, flat road and finish surfaces, prototype visual height separation, compact screen-space player marker constants, required marker RectTransform parent validation, disabled world player mesh rendering, inward side-lane spacing, disabled low gate footprint decals, disabled world-shot tracers, high gate and zombie card constants, and angled perspective camera framing constants.
- `PhaseThreeHeroTests.cs`: Verifies gameplay hero rewards, hero XP/level progression, manual coin level-up, manual hero equip state, hero stat bonuses, save persistence, and invalid equipped hero repair.
- `PhaseTwoProgressionTests.cs`: Verifies local coin collection, minigame win rewards, HQ upgrade costs and timers, HQ completion, mission normalization/selection/completion/unlocks, mission status/reward hints, invalid timer repair, minigame bonuses, save reset, and save/load persistence.

## Compile

Unity compiles and runs these tests through the `LaneSurvivor.Tests.EditMode` Unity Test Framework assembly.

## Behavior

The tests focus on small deterministic gameplay and progression rules that do not require entering Play Mode.

For scene-level flow checks, see `Assets/Tests/PlayMode`.
