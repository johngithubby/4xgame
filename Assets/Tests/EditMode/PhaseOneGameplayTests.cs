using LaneSurvivor.Data;
using LaneSurvivor.Gameplay;
using LaneSurvivor.Progression;
using LaneSurvivor.Rendering;
using LaneSurvivor.Save;
using LaneSurvivor.UI;
using NUnit.Framework;
using System.Reflection;
using UnityEngine;
using UnityEngine.Rendering;

namespace LaneSurvivor.Tests.EditMode
{
    public sealed class PhaseOneGameplayTests
    {
        [Test]
        public void GateModifiers_UpdateSquadCountAndDamage()
        {
            PlayerSquad squad = CreateSquad(5, 1f);

            squad.ApplyGate(GateModifierType.AddSquad, 10, 0f);
            Assert.AreEqual(15, squad.SquadCount);

            squad.ApplyGate(GateModifierType.MultiplySquad, 2, 0f);
            Assert.AreEqual(30, squad.SquadCount);

            squad.ApplyGate(GateModifierType.SubtractSquad, 35, 0f);
            Assert.AreEqual(0, squad.SquadCount);

            squad.ApplyGate(GateModifierType.AddDamage, 0, 2.5f);
            Assert.AreEqual(3.5f, squad.DamagePerMember);

            squad.ApplyGate(GateModifierType.MultiplyDamage, 0, 2f);
            Assert.AreEqual(7f, squad.DamagePerMember);
        }

        [Test]
        public void GateResolution_OnlyAppliesWhenSquadIsInGateLane()
        {
            PlayerSquad squad = CreateSquad(5, 1f);
            Gate missedGate = CreateGate(GateModifierType.AddSquad, 10, 0f, new Vector3(2f, 1f, 0f));

            squad.transform.position = new Vector3(0f, 1f, 1f);
            bool missedGateResolved = missedGate.TryResolve(squad, 0.5f);

            Assert.IsTrue(missedGateResolved);
            Assert.AreEqual(5, squad.SquadCount);
            Assert.IsTrue(missedGate.HasResolved);
            Assert.IsFalse(missedGate.LastResolutionApplied);

            Gate hitGate = CreateGate(GateModifierType.AddSquad, 10, 0f, new Vector3(0f, 1f, 0f));
            bool hitGateResolved = hitGate.TryResolve(squad, 0.5f);

            Assert.IsTrue(hitGateResolved);
            Assert.AreEqual(15, squad.SquadCount);
            Assert.IsTrue(hitGate.HasResolved);
            Assert.IsTrue(hitGate.LastResolutionApplied);
        }

        [Test]
        public void GateResolution_KeepsGateVisualsBrieflyAfterContact()
        {
            // Resolved gates should remain visible for only a short contact flash after contact.
            PlayerSquad squad = CreateSquad(5, 1f);
            Material material = PrototypeMaterialFactory.Create(Color.red);
            GameObject gateObject = PrototypeGeometryFactory.CreateCube("Renderable Gate Under Test", Vector3.zero, Vector3.one, material);
            GameObject labelObject = new("Gate Label Under Test");
            TextMesh label = labelObject.AddComponent<TextMesh>();

            try
            {
                // The label mirrors runtime gate construction by living under the gate object.
                labelObject.transform.SetParent(gateObject.transform, false);

                // The gate sits beside the squad and at the squad's current Z so it exercises the miss path.
                GateSpawnDefinition gateDefinition = new()
                {
                    modifierType = GateModifierType.SubtractSquad,
                    squadValue = 1,
                    damageValue = 0f,
                    position = new Vector3(GameplayVisuals.SideLaneX, 0f, 0f)
                };

                // Configure attaches gate data and the label text to the renderable test object.
                Gate gate = gateObject.AddComponent<Gate>();
                gate.Configure(gateDefinition, material, label);

                // Move to the gate center so TryResolve follows the contact path of a real run.
                squad.transform.position = new Vector3(0f, 1f, 0f);
                Assert.IsTrue(gate.TryResolve(squad, 0.5f));
                Assert.IsFalse(gate.LastResolutionApplied);

                // The object stays active immediately after contact so it does not vanish while approaching.
                Assert.IsTrue(gateObject.activeSelf);

                // The flash must be short enough that the trailing camera cannot let the old gate occlude the squad.
                Assert.LessOrEqual(Gate.ResolvedVisualLifetimeSeconds, 0.2f);

                // A tiny positive lifetime still leaves a visible contact beat instead of an instant one-frame pop.
                Assert.Greater(Gate.ResolvedVisualLifetimeSeconds, 0.05f);

                // A miss should keep the subtract color instead of tinting almost black and disappearing visually.
                AssertMaterialColor(new Color(0.90f, 0.18f, 0.16f), gateObject.GetComponent<Renderer>().sharedMaterial);
            }
            finally
            {
                // Destroy generated Unity objects explicitly so EditMode tests stay isolated.
                UnityEngine.Object.DestroyImmediate(gateObject);
                UnityEngine.Object.DestroyImmediate(squad.gameObject);
                UnityEngine.Object.DestroyImmediate(material);
            }
        }

        [Test]
        public void GateConfigure_PreservesLabelMaterialDuringTinting()
        {
            // Gate tinting should not replace the TextMesh font material or labels become same-color-on-same-color.
            PlayerSquad squad = CreateSquad(5, 1f);
            Material sourceMaterial = PrototypeMaterialFactory.Create(Color.yellow);
            GameObject gateObject = PrototypeGeometryFactory.CreateCube("Gate Label Material Under Test", Vector3.zero, Vector3.one, sourceMaterial);
            GameObject labelObject = new("Gate Label Under Test");
            TextMesh label = labelObject.AddComponent<TextMesh>();
            Renderer labelRenderer = labelObject.GetComponent<Renderer>();
            Assert.IsNotNull(labelRenderer);
            Material originalLabelMaterial = labelRenderer.sharedMaterial;

            try
            {
                // The generated runtime scene parents the TextMesh under the gate root before configuration.
                labelObject.transform.SetParent(gateObject.transform, false);

                // A renderable gate exercises the owned-material path that previously overwrote child text renderers.
                GateSpawnDefinition gateDefinition = new()
                {
                    modifierType = GateModifierType.AddDamage,
                    squadValue = 0,
                    damageValue = 0.5f,
                    position = Vector3.zero
                };

                // Configure should tint only gate marker meshes, not the label renderer.
                Gate gate = gateObject.AddComponent<Gate>();
                gate.Configure(gateDefinition, sourceMaterial, label);

                // The gate body should receive an owned tinted material clone.
                Material bodyMaterial = gateObject.GetComponent<Renderer>().sharedMaterial;
                Assert.IsNotNull(bodyMaterial);

                // The label should keep its original font material instead of sharing the gate body material.
                Assert.AreSame(originalLabelMaterial, labelRenderer.sharedMaterial);
                Assert.AreNotSame(bodyMaterial, labelRenderer.sharedMaterial);

                // Resolve tinting should also leave the label font material alone.
                squad.transform.position = Vector3.zero;
                Assert.IsTrue(gate.TryResolve(squad, 0.5f));
                Assert.AreSame(originalLabelMaterial, labelRenderer.sharedMaterial);
            }
            finally
            {
                // Destroy generated Unity objects explicitly so EditMode tests stay isolated.
                UnityEngine.Object.DestroyImmediate(gateObject);
                UnityEngine.Object.DestroyImmediate(squad.gameObject);
                UnityEngine.Object.DestroyImmediate(sourceMaterial);
            }
        }

        [Test]
        public void GateResolution_WaitsUntilSquadCenterReachesGate()
        {
            // Gates should not resolve early because early disappearance reads as a visual bug.
            PlayerSquad squad = CreateSquad(5, 1f);
            Gate gate = CreateGate(GateModifierType.AddSquad, 2, 0f, new Vector3(0f, 1f, 10f));

            // Any position before the gate center should leave the gate visible and unresolved.
            squad.transform.position = new Vector3(0f, 1f, 9.95f);
            Assert.IsFalse(gate.TryResolve(squad, 0.5f));
            Assert.AreEqual(5, squad.SquadCount);

            // Contact at the gate center applies the modifier and marks the gate resolved.
            squad.transform.position = new Vector3(0f, 1f, 10f);
            Assert.IsTrue(gate.TryResolve(squad, 0.5f));
            Assert.AreEqual(7, squad.SquadCount);
            Assert.IsTrue(gate.HasResolved);
        }

        [Test]
        public void LaneMovement_ClampsToConfiguredLaneRange()
        {
            PlayerSquad squad = CreateSquad(5, 1f);

            squad.MoveLane(-1);
            squad.MoveLane(-1);
            Assert.AreEqual(0, squad.CurrentLaneIndex);

            squad.MoveLane(1);
            squad.MoveLane(1);
            squad.MoveLane(1);
            Assert.AreEqual(2, squad.CurrentLaneIndex);
        }

        [Test]
        public void LevelManager_EnablesDepthSafeWorldSpaceCombatFeedback()
        {
            // World-space TextMesh labels should be restored now that they use depth-safe foreground materials.
            Assert.IsTrue(LevelManager.WorldSpaceFeedbackEnabled);

            // World-space tracers should be restored as thin flat mesh strips, not gate-like cube geometry.
            Assert.IsTrue(LevelManager.WorldSpaceShotTracersEnabled);
        }

        [Test]
        public void PrototypeMaterialFactory_CreatesAlwaysVisibleFeedbackMaterial()
        {
            // Combat effects need their own transparent material contract instead of borrowing opaque geometry state.
            Material material = PrototypeMaterialFactory.CreateAlwaysVisibleFeedback(Color.yellow);

            try
            {
                // The iOS feedback path must resolve to a real shader in player builds.
                Assert.IsNotNull(material.shader);

                // Foreground feedback should render after opaque road, gate, and zombie cards.
                Assert.GreaterOrEqual(material.renderQueue, (int)RenderQueue.Overlay);

                // The transparent render type keeps feedback out of the opaque depth-writing pass.
                Assert.AreEqual("Transparent", material.GetTag("RenderType", false, string.Empty));

                // Feedback should not write depth because that can hide later world objects.
                AssertMaterialFloatIfPresent(material, "_ZWrite", 0f);

                // When the shader exposes depth tests, feedback must ignore scene depth for readability.
                AssertMaterialFloatIfPresent(material, "_ZTest", (float)CompareFunction.Always);

                // Alpha blending keeps transparent labels and text shadows from becoming opaque obstacle blocks.
                AssertMaterialFloatIfPresent(material, "_SrcBlend", (float)BlendMode.SrcAlpha);
                AssertMaterialFloatIfPresent(material, "_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
            }
            finally
            {
                // Destroy the generated material explicitly so the EditMode test does not leak Unity objects.
                UnityEngine.Object.DestroyImmediate(material);
            }
        }

        [Test]
        public void PrototypeMaterialFactory_CreatesAlwaysVisibleSolidFeedbackMaterial()
        {
            // Shot tracer strips use a solid foreground material because that path is reliable in iOS player builds.
            Material material = PrototypeMaterialFactory.CreateAlwaysVisibleSolidFeedback(new Color(1f, 0.4f, 0f));

            try
            {
                // The solid feedback path must resolve to a real shader.
                Assert.IsNotNull(material.shader);

                // Solid tracer strips still draw late so the road pass cannot cover them.
                Assert.GreaterOrEqual(material.renderQueue, (int)RenderQueue.Overlay);

                // Solid feedback should not write depth because it is not level geometry.
                AssertMaterialFloatIfPresent(material, "_ZWrite", 0f);

                // Compatible shaders should draw tracer strips regardless of depth ordering.
                AssertMaterialFloatIfPresent(material, "_ZTest", (float)CompareFunction.Always);

                // Solid feedback should replace color instead of entering transparent alpha sorting.
                AssertMaterialFloatIfPresent(material, "_SrcBlend", (float)BlendMode.One);
                AssertMaterialFloatIfPresent(material, "_DstBlend", (float)BlendMode.Zero);
            }
            finally
            {
                // Destroy the generated material explicitly so the EditMode test does not leak Unity objects.
                UnityEngine.Object.DestroyImmediate(material);
            }
        }

        [Test]
        public void AutoShooter_DamagesZombieAheadInSameLane()
        {
            // Create a moving squad so the shooter follows its normal runtime guard conditions.
            PlayerSquad squad = CreateSquad(5, 1f);
            AutoShooter shooter = squad.gameObject.AddComponent<AutoShooter>();
            Zombie zombie = CreateZombie(new Vector3(0f, GameplayVisuals.ZombieCenterY, 4f), 1);
            bool shotFired = false;

            try
            {
                // The registered target is ahead, in range, and in the squad lane.
                shooter.Initialize(squad, 8f, 0.35f, 0.5f);
                shooter.RegisterZombie(zombie);
                shooter.ShotFired += (_, _, _, _) => shotFired = true;
                squad.SetMoving(true);

                // Reflection invokes the Unity Update callback without requiring a PlayMode frame.
                InvokeAutoShooterUpdate(shooter);

                // Five squad members at one damage each should defeat the five-health target in one volley.
                Assert.IsTrue(shotFired);
                Assert.IsTrue(zombie.IsDefeated);
            }
            finally
            {
                // Destroy generated Unity objects explicitly so EditMode tests stay isolated.
                UnityEngine.Object.DestroyImmediate(squad.gameObject);
                UnityEngine.Object.DestroyImmediate(zombie.gameObject);
            }
        }

        [Test]
        public void AutoShooter_UsesGeneratedWeaponMuzzleWhenAvailable()
        {
            // Generated survivor weapons should drive shot origins instead of the old squad-root approximation.
            Material playerMaterial = PrototypeMaterialFactory.Create(Color.cyan);
            Material accentMaterial = PrototypeMaterialFactory.Create(Color.magenta);
            GameObject playerObject = PrototypeCharacterFactory.CreatePlayerSquad("Muzzle Squad Under Test", new Vector3(0f, GameplayVisuals.PlayerCenterY, 0f), playerMaterial, accentMaterial);
            PlayerSquad squad = playerObject.AddComponent<PlayerSquad>();
            AutoShooter shooter = playerObject.AddComponent<AutoShooter>();
            Zombie zombie = CreateZombie(new Vector3(0f, GameplayVisuals.ZombieCenterY, 4f), 1);
            LevelDefinition levelDefinition = ScriptableObject.CreateInstance<LevelDefinition>();
            bool shotFired = false;
            bool detailedShotFired = false;
            bool originUsedWeaponMuzzle = false;
            Vector3 actualOrigin = default;
            Transform actualMuzzle = null;

            try
            {
                // The authored lane setup matches runtime enough to initialize the squad and shooter deterministically.
                levelDefinition.startingSquadCount = 5;
                levelDefinition.startingDamagePerMember = 1f;
                levelDefinition.squadMoveSpeed = 1f;
                levelDefinition.lanePositions = new[] { -GameplayVisuals.SideLaneX, 0f, GameplayVisuals.SideLaneX };

                // The imported visible weapon supersedes hidden generated wing anchors in the live firing registry.
                squad.Initialize(levelDefinition);
                Assert.AreEqual(1, squad.WeaponMuzzleCount);

                // The first round-robin shot should come from the imported leader's actual visible barrel tip.
                Transform leaderSwatModel = playerObject.transform.Find($"Survivor Leader/{PrototypeCharacterFactory.SwatSurvivorModelName}");
                Assert.IsNotNull(leaderSwatModel);
                Transform expectedMuzzle = FindNamedDescendant(leaderSwatModel, PlayerSquad.WeaponMuzzleAnchorName);
                Assert.IsNotNull(expectedMuzzle);
                Transform leaderWeapon = playerObject.transform.Find($"Survivor Leader/Human Arm Right/Human Hand Right/{PrototypeCharacterFactory.LeaderRifleName}");
                Assert.IsNotNull(leaderWeapon);
                Transform hiddenGeneratedMuzzle = leaderWeapon.Find(PrototypeCharacterFactory.HiddenGeneratedWeaponMuzzleName);
                Assert.IsNotNull(hiddenGeneratedMuzzle);

                // Repeated selection must stay on the only rendered rifle instead of emitting from invisible wing soldiers.
                Assert.IsTrue(squad.TryGetNextWeaponMuzzle(out Transform firstRegisteredMuzzle));
                Assert.AreSame(expectedMuzzle, firstRegisteredMuzzle);
                Assert.IsTrue(squad.TryGetNextWeaponMuzzle(out Transform repeatedRegisteredMuzzle));
                Assert.AreSame(expectedMuzzle, repeatedRegisteredMuzzle);
                Vector3 leaderWeaponRestPosition = leaderWeapon.localPosition;
                Quaternion leaderWeaponRestRotation = leaderWeapon.localRotation;
                PrototypeHumanoidAnimator playerAnimator = playerObject.GetComponent<PrototypeHumanoidAnimator>();
                Assert.IsNotNull(playerAnimator);
                SwatSurvivorLocomotionAnimator swatLocomotion = leaderSwatModel.GetComponent<SwatSurvivorLocomotionAnimator>();
                Assert.IsNotNull(swatLocomotion);

                // Configure the same target path used by normal gameplay.
                shooter.Initialize(squad, 8f, 0.35f, 0.5f);
                shooter.RegisterZombie(zombie);
                shooter.ShotFired += (origin, _, _, usesWeaponMuzzle) =>
                {
                    shotFired = true;
                    actualOrigin = origin;
                    originUsedWeaponMuzzle = usesWeaponMuzzle;
                };
                shooter.ShotFiredDetailed += (origin, _, _, usesWeaponMuzzle, muzzle) =>
                {
                    detailedShotFired = true;
                    actualMuzzle = muzzle;
                    Assert.IsTrue(usesWeaponMuzzle);
                    Assert.AreEqual(actualOrigin.x, origin.x, 0.001f);
                    Assert.AreEqual(actualOrigin.y, origin.y, 0.001f);
                    Assert.AreEqual(actualOrigin.z, origin.z, 0.001f);
                };
                squad.SetMoving(true);

                // Reflection invokes the Unity Update callback without needing a PlayMode frame.
                InvokeAutoShooterUpdate(shooter);

                // Shot events should now expose the exact muzzle world position and mark it as weapon-based.
                Assert.IsTrue(shotFired);
                Assert.IsTrue(originUsedWeaponMuzzle);
                Assert.AreEqual(expectedMuzzle.position.x, actualOrigin.x, 0.001f);
                Assert.AreEqual(expectedMuzzle.position.y, actualOrigin.y, 0.001f);
                Assert.AreEqual(expectedMuzzle.position.z, actualOrigin.z, 0.001f);
                Assert.IsTrue(detailedShotFired);
                Assert.AreSame(expectedMuzzle, actualMuzzle);

                // AutoShooter must rotate the visible imported barrel toward the exact zombie point before firing events.
                Vector3 zombieTargetPoint = zombie.transform.position + Vector3.up * 0.5f;
                Assert.LessOrEqual(swatLocomotion.WeaponAimErrorDegrees, SwatSurvivorLocomotionAnimator.MaximumWeaponAimErrorDegrees);
                Assert.LessOrEqual(Vector3.Angle(expectedMuzzle.forward, zombieTargetPoint - expectedMuzzle.position), SwatSurvivorLocomotionAnimator.MaximumWeaponAimErrorDegrees);

                // The obsolete generated carrier stays stable because the imported SWAT MPX pivot now owns visible aim.
                playerAnimator.ForceEvaluate(0.02f, true);
                Assert.Less(Vector3.Distance(leaderWeaponRestPosition, leaderWeapon.localPosition), 0.001f);
                Assert.Less(Quaternion.Angle(leaderWeaponRestRotation, leaderWeapon.localRotation), 0.001f);
            }
            finally
            {
                // Destroy generated Unity objects explicitly so EditMode tests stay isolated.
                UnityEngine.Object.DestroyImmediate(playerObject);
                UnityEngine.Object.DestroyImmediate(zombie.gameObject);
                UnityEngine.Object.DestroyImmediate(playerMaterial);
                UnityEngine.Object.DestroyImmediate(accentMaterial);
                UnityEngine.Object.DestroyImmediate(levelDefinition);
            }
        }

        [Test]
        public void AutoShooter_IgnoresZombieAheadInDifferentLane()
        {
            // Create a moving squad so the shooter is allowed to search for targets.
            PlayerSquad squad = CreateSquad(5, 1f);
            AutoShooter shooter = squad.gameObject.AddComponent<AutoShooter>();
            Zombie zombie = CreateZombie(new Vector3(GameplayVisuals.SideLaneX, GameplayVisuals.ZombieCenterY, 4f), 1);
            bool shotFired = false;

            try
            {
                // The target is ahead and in range, but the strict tolerance keeps it outside the center lane.
                shooter.Initialize(squad, 8f, 0.35f, 0.1f);
                shooter.RegisterZombie(zombie);
                shooter.ShotFired += (_, _, _, _) => shotFired = true;
                squad.SetMoving(true);

                // Reflection invokes the same private Update method Unity calls every frame.
                InvokeAutoShooterUpdate(shooter);

                // Cross-lane targets should survive until the player switches lanes.
                Assert.IsFalse(shotFired);
                Assert.IsFalse(zombie.IsDefeated);
            }
            finally
            {
                // Destroy generated Unity objects explicitly so EditMode tests stay isolated.
                UnityEngine.Object.DestroyImmediate(squad.gameObject);
                UnityEngine.Object.DestroyImmediate(zombie.gameObject);
            }
        }

        [Test]
        public void Zombie_TakesDamageAndReportsDefeat()
        {
            GameObject zombieObject = new("Zombie Under Test");

            Zombie zombie = zombieObject.AddComponent<Zombie>();
            zombie.Configure(5f, 1, null);

            bool defeated = false;
            zombie.Defeated += _ => defeated = true;

            zombie.TakeDamage(5f);

            Assert.IsTrue(zombie.IsDefeated);
            Assert.IsTrue(defeated);
        }

        [Test]
        public void ArmoredZombie_ReducesIncomingShotDamage()
        {
            GameObject zombieObject = new("Armored Zombie Under Test");

            Zombie zombie = zombieObject.AddComponent<Zombie>();
            zombie.Configure(10f, 1, null, ZombieEnemyType.Armored);

            float appliedDamage = zombie.TakeDamage(5f);

            Assert.AreEqual(3f, appliedDamage, 0.001f);
            Assert.IsFalse(zombie.IsDefeated);

            zombie.TakeDamage(20f);

            Assert.IsTrue(zombie.IsDefeated);
        }

        [Test]
        public void ZombieBreach_OnlyDamagesSquadInSameLane()
        {
            PlayerSquad squad = CreateSquad(5, 1f);
            Zombie zombie = CreateZombie(new Vector3(2f, 1f, 0f), 3);

            squad.transform.position = new Vector3(0f, 1f, 1f);
            bool resolved = zombie.TryBreach(squad, 0.5f);

            Assert.IsTrue(resolved);
            Assert.IsTrue(zombie.IsDefeated);
            Assert.IsFalse(zombie.LastBreachApplied);
            Assert.AreEqual(5, squad.SquadCount);
        }

        [Test]
        public void LevelStateEvaluator_WinsAtFinishDistance()
        {
            LevelState state = LevelStateEvaluator.Evaluate(50f, 48f, 1, LevelState.Playing);

            Assert.AreEqual(LevelState.Won, state);
        }

        [Test]
        public void LevelStateEvaluator_LosesWhenSquadCountReachesZero()
        {
            LevelState state = LevelStateEvaluator.Evaluate(10f, 48f, 0, LevelState.Playing);

            Assert.AreEqual(LevelState.Lost, state);
        }

        [Test]
        public void LevelDefinitionFactory_UsesUnlockedSecondLevel()
        {
            SaveGameData saveData = new()
            {
                unlockedMinigameLevel = 2
            };

            LevelDefinition levelDefinition = LevelDefinitionFactory.CreateForSave(saveData);

            Assert.AreEqual(2, levelDefinition.levelNumber);
            Assert.Greater(levelDefinition.finishDistance, 48f);
            Assert.AreEqual(4, levelDefinition.zombies.Length);
        }

        [Test]
        public void LevelDefinitionFactory_UsesSelectedMissionLevel()
        {
            SaveGameData saveData = new()
            {
                currentMissionLevel = 3,
                highestUnlockedMissionLevel = 4
            };

            LevelDefinition levelDefinition = LevelDefinitionFactory.CreateForSave(saveData);

            Assert.AreEqual(3, levelDefinition.levelNumber);
            Assert.AreEqual(5, levelDefinition.gates.Length);
            Assert.AreEqual(5, levelDefinition.zombies.Length);
        }

        [Test]
        public void LevelDefinitionFactory_ProvidesFourthMissionAtLocalCap()
        {
            SaveGameData saveData = new()
            {
                currentMissionLevel = 4,
                highestUnlockedMissionLevel = 4
            };

            LevelDefinition levelDefinition = LevelDefinitionFactory.CreateForSave(saveData);

            Assert.AreEqual(4, levelDefinition.levelNumber);
            Assert.AreEqual(6, levelDefinition.gates.Length);
            Assert.AreEqual(6, levelDefinition.zombies.Length);
            Assert.Greater(levelDefinition.finishDistance, 70f);
        }

        [Test]
        public void LevelDefinitionFactory_ProvidesEighthMissionWithAdvancedContent()
        {
            SaveGameData saveData = new()
            {
                currentMissionLevel = PlayerProgression.MaxMissionLevel,
                highestUnlockedMissionLevel = PlayerProgression.MaxMissionLevel
            };

            LevelDefinition levelDefinition = LevelDefinitionFactory.CreateForSave(saveData);

            Assert.AreEqual(PlayerProgression.MaxMissionLevel, levelDefinition.levelNumber);
            Assert.AreEqual(9, levelDefinition.gates.Length);
            Assert.AreEqual(10, levelDefinition.zombies.Length);
            Assert.Greater(levelDefinition.finishDistance, 100f);
            Assert.IsTrue(System.Array.Exists(levelDefinition.gates, gate => gate.modifierType == GateModifierType.MultiplyDamage));
            Assert.IsTrue(System.Array.Exists(levelDefinition.zombies, zombie => zombie.enemyType == ZombieEnemyType.Armored));
        }

        [Test]
        public void PrototypeMaterialFactory_CreatesTintableMaterial()
        {
            // The factory must not depend on any one named shader being present in a player build.
            Material material = PrototypeMaterialFactory.Create(Color.magenta);

            try
            {
                // The iOS deploy path failed when Material was constructed with a null shader.
                Assert.IsNotNull(material.shader);

                // Placeholder materials need at least one common tint property for visual debugging.
                Assert.IsTrue(material.HasProperty("_BaseColor") || material.HasProperty("_Color"));

                // World placeholder geometry must render in the opaque queue so the track cannot draw over late gates.
                Assert.LessOrEqual(material.renderQueue, (int)RenderQueue.GeometryLast);

                // Runtime 3D placeholders should never use transparent UI or sprite fallback shaders.
                Assert.AreNotEqual("Sprites/Default", material.shader.name);
                Assert.AreNotEqual("UI/Default", material.shader.name);

                // Compatible shaders expose this tag to replacement passes and render pipeline classification.
                Assert.AreEqual("Opaque", material.GetTag("RenderType", false, string.Empty));

                // URP shaders expose _Surface, where opaque is zero.
                AssertMaterialFloatIfPresent(material, "_Surface", 0f);

                // Built-in Standard exposes _Mode, where opaque is zero.
                AssertMaterialFloatIfPresent(material, "_Mode", 0f);

                // Blend One/Zero keeps generated world placeholders out of transparent alpha composition.
                AssertMaterialFloatIfPresent(material, "_SrcBlend", (float)BlendMode.One);
                AssertMaterialFloatIfPresent(material, "_DstBlend", (float)BlendMode.Zero);

                // Depth writes prevent a large generated road plane from hiding distant gate cards on iOS/Metal.
                AssertMaterialFloatIfPresent(material, "_ZWrite", 1f);
            }
            finally
            {
                // Destroy the material explicitly so the EditMode test does not leak Unity objects.
                UnityEngine.Object.DestroyImmediate(material);
            }
        }

        [Test]
        public void PrototypeGeometryFactory_CreatesRenderableCubeWithoutColliderComponent()
        {
            // Runtime placeholder geometry should render without using Unity's primitive collider path.
            Material material = PrototypeMaterialFactory.Create(Color.yellow);
            GameObject cube = PrototypeGeometryFactory.CreateCube("Cube Under Test", Vector3.one, Vector3.one * 2f, material);

            try
            {
                // The factory must attach a mesh filter so the object has geometry to draw.
                Assert.IsNotNull(cube.GetComponent<MeshFilter>());

                // The factory must attach a renderer so caller-provided materials appear in scenes.
                Assert.IsNotNull(cube.GetComponent<MeshRenderer>());

                // Transform values should match the factory arguments exactly.
                Assert.AreEqual(Vector3.one, cube.transform.position);
                Assert.AreEqual(Vector3.one * 2f, cube.transform.localScale);

                // Avoid a direct Collider type reference because this project may compile without Physics.
                AssertNoColliderComponents(cube);
            }
            finally
            {
                // Destroy generated Unity objects explicitly so EditMode tests stay isolated.
                UnityEngine.Object.DestroyImmediate(cube);
                UnityEngine.Object.DestroyImmediate(material);
            }
        }

        [Test]
        public void PrototypeGeometryFactory_CreatesFlatRoadSurfaceWithoutColliderComponent()
        {
            // Road geometry should be a flat render surface so it cannot hide actors from the camera.
            Material material = PrototypeMaterialFactory.Create(Color.green);
            GameObject plane = PrototypeGeometryFactory.CreateHorizontalPlane("Plane Under Test", Vector3.zero, new Vector2(7f, 48f), material);

            try
            {
                // The factory must attach renderable mesh components without using Unity primitives.
                Assert.IsNotNull(plane.GetComponent<MeshFilter>());
                Assert.IsNotNull(plane.GetComponent<MeshRenderer>());

                // The surface uses X/Z scale while leaving Y unscaled as a flat mesh plane.
                Assert.AreEqual(new Vector3(7f, 1f, 48f), plane.transform.localScale);

                // Avoid a direct Collider type reference because this project may compile without Physics.
                AssertNoColliderComponents(plane);
            }
            finally
            {
                // Destroy generated Unity objects explicitly so EditMode tests stay isolated.
                UnityEngine.Object.DestroyImmediate(plane);
                UnityEngine.Object.DestroyImmediate(material);
            }
        }

        [Test]
        public void PrototypeGeometryFactory_CreatesRoundedPrimitivesWithoutColliderComponents()
        {
            // Humanoid characters should use generated rounded meshes, not Unity primitive objects with colliders.
            Material material = PrototypeMaterialFactory.Create(Color.cyan);
            GameObject sphere = PrototypeGeometryFactory.CreateSphere("Sphere Under Test", Vector3.zero, Vector3.one, material);
            GameObject cylinder = PrototypeGeometryFactory.CreateCylinder("Cylinder Under Test", Vector3.right, Vector3.one, material);

            try
            {
                // Both primitives need renderable mesh data for generated character parts.
                Assert.IsNotNull(sphere.GetComponent<MeshFilter>()?.sharedMesh);
                Assert.IsNotNull(cylinder.GetComponent<MeshFilter>()?.sharedMesh);

                // Rounded meshes should have more geometry than the old eight-corner cube silhouette.
                Assert.Greater(sphere.GetComponent<MeshFilter>().sharedMesh.vertexCount, 24);
                Assert.Greater(cylinder.GetComponent<MeshFilter>().sharedMesh.vertexCount, 24);

                // Avoid a direct Collider type reference because this project may compile without Physics.
                AssertNoColliderComponents(sphere);
                AssertNoColliderComponents(cylinder);
            }
            finally
            {
                // Destroy generated Unity objects explicitly so EditMode tests stay isolated.
                UnityEngine.Object.DestroyImmediate(sphere);
                UnityEngine.Object.DestroyImmediate(cylinder);
                UnityEngine.Object.DestroyImmediate(material);
            }
        }

        [Test]
        public void PrototypeCharacterFactory_BuildsHumanoidPlayerAndZombieParts()
        {
            // Character generation should replace old rectangular actor blobs with recognizable body hierarchies.
            Material playerMaterial = PrototypeMaterialFactory.Create(Color.cyan);
            Material accentMaterial = PrototypeMaterialFactory.Create(Color.magenta);
            Material zombieMaterial = PrototypeMaterialFactory.Create(Color.green);
            GameObject player = PrototypeCharacterFactory.CreatePlayerSquad("Player Squad Under Test", new Vector3(0f, GameplayVisuals.PlayerCenterY, 0f), playerMaterial, accentMaterial);
            GameObject zombie = PrototypeCharacterFactory.CreateZombie("Zombie Under Test", Vector3.forward, zombieMaterial, ZombieEnemyType.Basic);
            GameObject armoredZombie = PrototypeCharacterFactory.CreateZombie("Armored Zombie Under Test", Vector3.forward * 2f, zombieMaterial, ZombieEnemyType.Armored);

            try
            {
                // The player root remains a gameplay anchor while child meshes create the visible squad.
                Assert.IsNull(player.GetComponent<MeshFilter>());
                Assert.IsNotNull(player.transform.Find("Survivor Leader/Human Head"));
                Assert.IsNotNull(player.transform.Find("Survivor Leader/Human Hood Collar"));
                Assert.IsNotNull(player.transform.Find("Survivor Leader/Human Chest Armor"));
                Assert.IsNotNull(player.transform.Find("Survivor Leader/Human Chest Glow"));
                Assert.IsNotNull(player.transform.Find("Survivor Leader/Human Backpack"));
                Assert.IsNotNull(player.transform.Find("Survivor Leader/Human Backpack Antenna"));
                Assert.IsNotNull(player.transform.Find("Survivor Leader/Human Rear Jacket Panel"));
                Assert.IsNotNull(player.transform.Find("Survivor Leader/Human Rear Shoulder Plate"));
                Assert.IsNotNull(player.transform.Find("Survivor Leader/Human Rear Strap Left"));
                Assert.IsNotNull(player.transform.Find("Survivor Leader/Human Rear Strap Right"));
                Assert.IsNotNull(player.transform.Find("Survivor Leader/Human Rear Spine Glow"));
                Assert.IsNotNull(player.transform.Find("Survivor Leader/Human Backpack Side Pod Left"));
                Assert.IsNotNull(player.transform.Find("Survivor Leader/Human Backpack Side Pod Right"));
                Assert.IsNotNull(player.transform.Find("Survivor Leader/Human Eye Left"));
                Assert.IsNotNull(player.transform.Find("Survivor Leader/Human Nose"));
                Assert.IsNotNull(player.transform.Find("Survivor Leader/Human Hair Back"));
                Assert.IsNotNull(player.transform.Find("Survivor Leader/Human Hair Left"));
                Assert.IsNotNull(player.transform.Find("Survivor Leader/Human Hair Right"));
                Assert.IsNotNull(player.transform.Find("Survivor Leader/Human Ponytail Base"));
                Assert.IsNotNull(player.transform.Find("Survivor Leader/Human Ponytail Upper"));
                Assert.IsNotNull(player.transform.Find("Survivor Leader/Human Ponytail Tip"));
                Assert.IsNotNull(player.transform.Find("Survivor Leader/Human Chin Shadow"));
                Assert.IsNull(player.transform.Find("Survivor Leader/Human Beard"));
                Assert.IsNotNull(player.transform.Find("Survivor Leader/Human Helmet Brim"));
                Assert.IsNotNull(player.transform.Find("Survivor Leader/Human Helmet Glow"));
                Assert.IsNotNull(player.transform.Find("Survivor Leader/Human Helmet Top Seam"));
                Assert.IsNotNull(player.transform.Find("Survivor Leader/Human Helmet Rear Glow"));
                Assert.IsNotNull(player.transform.Find("Survivor Leader/Human Headset Mic"));
                Assert.IsNotNull(player.transform.Find("Survivor Leader/Human Leg Left/Human Knee Left"));
                Assert.IsNotNull(player.transform.Find("Survivor Leader/Human Leg Left/Human Knee Left/Human Shin Left"));
                Assert.IsNotNull(player.transform.Find("Survivor Leader/Human Arm Left/Human Shoulder Armor Left"));
                Assert.IsNotNull(player.transform.Find("Survivor Leader/Human Arm Left/Human Forearm Glow Left"));
                Assert.IsNotNull(player.transform.Find("Survivor Leader/Human Leg Left/Human Thigh Holster Left"));
                Assert.IsNotNull(player.transform.Find("Survivor Leader/Human Leg Left/Human Knee Left/Human Knee Armor Left"));
                Assert.IsNotNull(player.transform.Find("Survivor Leader/Human Leg Left/Human Knee Left/Human Shin Left/Human Boot Glow Left"));
                Assert.IsNotNull(player.transform.Find("Survivor Leader/Human Leg Left/Human Knee Left/Human Shin Left/Human Rear Shin Armor Left"));
                Assert.IsNotNull(player.transform.Find("Survivor Leader/Human Leg Left/Human Knee Left/Human Shin Left/Human Rear Boot Glow Left"));
                Assert.IsNull(player.transform.Find($"Survivor Leader/{PrototypeCharacterFactory.SoldierReferenceVisualName}"));
                Assert.IsNull(player.transform.Find($"Survivor Left Wing/{PrototypeCharacterFactory.SoldierReferenceVisualName}"));
                Assert.IsNull(player.transform.Find($"Survivor Right Wing/{PrototypeCharacterFactory.SoldierReferenceVisualName}"));
                Assert.IsNull(player.transform.Find("Survivor Leader/Survivor Reference Shell"));
                Assert.IsNotNull(player.transform.Find($"Survivor Leader/{PrototypeCharacterFactory.FemaleReferenceUpperName}"));
                Assert.IsNull(player.transform.Find($"Survivor Leader/Human Leg Left/{PrototypeCharacterFactory.FemaleReferenceLeftLegName}"));
                Assert.IsNull(player.transform.Find($"Survivor Leader/Human Leg Right/{PrototypeCharacterFactory.FemaleReferenceRightLegName}"));
                Assert.IsNull(player.transform.Find($"Survivor Leader/Human Arm Right/Human Hand Right/{PrototypeCharacterFactory.LeaderRifleName}/{PrototypeCharacterFactory.FemaleReferenceRifleName}"));
                Assert.IsNotNull(player.transform.Find($"Survivor Leader/{PrototypeCharacterFactory.FemaleReferenceUpperName}/{PrototypeCharacterFactory.FemaleReferenceRearName}"));

                // The survivor's local front must align with the muzzle side so rotation does not reveal an inside-out model.
                Assert.Greater(player.transform.Find("Survivor Leader/Human Nose").localPosition.z, 0f);
                Assert.Greater(player.transform.Find("Survivor Leader/Human Chest Armor").localPosition.z, 0f);
                Assert.Less(player.transform.Find("Survivor Leader/Human Backpack").localPosition.z, 0f);
                Assert.Less(player.transform.Find("Survivor Leader/Human Rear Jacket Panel").localPosition.z, 0f);
                Assert.Less(player.transform.Find("Survivor Leader/Human Rear Spine Glow").localPosition.z, 0f);
                Assert.Less(player.transform.Find("Survivor Leader/Human Helmet Rear Glow").localPosition.z, 0f);

                Assert.IsNotNull(player.transform.Find($"Survivor Leader/Human Arm Right/Human Hand Right/{PrototypeCharacterFactory.LeaderRifleName}"));
                Assert.IsNotNull(player.transform.Find($"Survivor Leader/Human Arm Right/Human Hand Right/{PrototypeCharacterFactory.LeaderRifleName}/Leader Rifle Receiver"));
                Assert.IsNotNull(player.transform.Find($"Survivor Leader/Human Arm Right/Human Hand Right/{PrototypeCharacterFactory.LeaderRifleName}/Leader Rifle Magazine"));
                Assert.IsNotNull(player.transform.Find($"Survivor Leader/Human Arm Right/Human Hand Right/{PrototypeCharacterFactory.LeaderRifleName}/Leader Rifle Side Plate"));
                Assert.IsNotNull(player.transform.Find($"Survivor Leader/Human Arm Right/Human Hand Right/{PrototypeCharacterFactory.LeaderRifleName}/Leader Rifle Sight Glow"));
                Assert.IsNotNull(player.transform.Find($"Survivor Leader/Human Arm Right/Human Hand Right/{PrototypeCharacterFactory.LeaderRifleName}/Leader Rifle Top Glow"));
                Assert.IsNotNull(player.transform.Find($"Survivor Left Wing/Human Arm Right/Human Hand Right/{PrototypeCharacterFactory.LeaderRifleName}"));
                Assert.IsNotNull(player.transform.Find($"Survivor Right Wing/Human Arm Right/Human Hand Right/{PrototypeCharacterFactory.LeaderRifleName}"));
                Assert.IsNull(player.transform.Find($"Survivor Left Wing/Human Arm Right/Human Hand Right/{PrototypeCharacterFactory.LeftWingShotgunName}"));
                Assert.IsNull(player.transform.Find($"Survivor Right Wing/Human Arm Right/Human Hand Right/{PrototypeCharacterFactory.RightWingSmgName}"));
                Assert.IsNotNull(player.transform.Find("Survivor Right Wing/Human Leg Right/Human Knee Right/Human Shin Right/Human Boot Right"));
                Assert.GreaterOrEqual(player.GetComponentsInChildren<MeshRenderer>().Length, 90);

                // The generated primitive rig stays hidden; the skinned soldier decals carry the visible raised rifle.
                MeshRenderer leaderHeadRenderer = player.transform.Find("Survivor Leader/Human Head").GetComponent<MeshRenderer>();
                MeshRenderer leaderWeaponRenderer = player.transform.Find($"Survivor Leader/Human Arm Right/Human Hand Right/{PrototypeCharacterFactory.LeaderRifleName}/Leader Rifle Body").GetComponent<MeshRenderer>();
                MeshRenderer leaderChestGlowRenderer = player.transform.Find("Survivor Leader/Human Chest Glow").GetComponent<MeshRenderer>();
                MeshRenderer leaderChestArmorRenderer = player.transform.Find("Survivor Leader/Human Chest Armor").GetComponent<MeshRenderer>();
                SkinnedMeshRenderer leaderReferenceRenderer = player.transform.Find($"Survivor Leader/{PrototypeCharacterFactory.FemaleReferenceUpperName}").GetComponent<SkinnedMeshRenderer>();
                SkinnedMeshRenderer leaderRearReferenceRenderer = player.transform.Find($"Survivor Leader/{PrototypeCharacterFactory.FemaleReferenceUpperName}/{PrototypeCharacterFactory.FemaleReferenceRearName}").GetComponent<SkinnedMeshRenderer>();
                Transform leaderSwatModel = player.transform.Find($"Survivor Leader/{PrototypeCharacterFactory.SwatSurvivorModelName}");
                Renderer leaderSwatSuitRenderer = FindNamedDescendant(leaderSwatModel, "Suit").GetComponent<Renderer>();
                Renderer leaderSwatBootsRenderer = FindNamedDescendant(leaderSwatModel, "Boots").GetComponent<Renderer>();
                Animator leaderSwatAnimator = leaderSwatModel.GetComponent<Animator>();
                SwatSurvivorLocomotionAnimator leaderSwatLocomotion = leaderSwatModel.GetComponent<SwatSurvivorLocomotionAnimator>();
                Assert.IsNotNull(leaderHeadRenderer);
                Assert.IsNotNull(leaderWeaponRenderer);
                Assert.IsNotNull(leaderChestGlowRenderer);
                Assert.IsNotNull(leaderChestArmorRenderer);
                Assert.IsNotNull(leaderReferenceRenderer);
                Assert.IsNotNull(leaderRearReferenceRenderer);
                Assert.IsNotNull(leaderSwatModel);
                Assert.IsNotNull(leaderSwatSuitRenderer);
                Assert.IsNotNull(leaderSwatBootsRenderer);
                Assert.IsNotNull(leaderSwatAnimator);
                Assert.IsNotNull(leaderSwatAnimator.runtimeAnimatorController);
                Assert.IsNotNull(leaderSwatLocomotion);
                Assert.IsFalse(leaderHeadRenderer.enabled);
                Assert.IsFalse(leaderWeaponRenderer.enabled);
                Assert.IsFalse(leaderChestGlowRenderer.enabled);
                Assert.IsFalse(leaderChestArmorRenderer.enabled);
                Assert.IsTrue(leaderSwatSuitRenderer.enabled);
                Assert.AreEqual("Standard", leaderSwatSuitRenderer.sharedMaterial.shader.name);
                Assert.IsNotNull(leaderSwatSuitRenderer.sharedMaterial.mainTexture);
                Assert.AreEqual("Outfit_Burglar2_Diffuse", leaderSwatSuitRenderer.sharedMaterial.mainTexture.name);
                Assert.IsNotNull(leaderSwatSuitRenderer.sharedMaterial.GetTexture("_BumpMap"));
                Assert.IsTrue(leaderSwatSuitRenderer.sharedMaterial.IsKeywordEnabled("_NORMALMAP"));
                Assert.AreEqual("Standard (Specular setup)", leaderSwatBootsRenderer.sharedMaterial.shader.name);
                Assert.AreEqual("Boots_Specular", leaderSwatBootsRenderer.sharedMaterial.GetTexture("_SpecGlossMap").name);
                Assert.IsTrue(leaderSwatBootsRenderer.sharedMaterial.IsKeywordEnabled("_SPECGLOSSMAP"));
                Assert.IsNotNull(leaderReferenceRenderer.GetComponent<ReferenceModelFacingVisibility>());
                Assert.AreEqual("FemaleSurvivorReferenceCutout", leaderReferenceRenderer.sharedMaterial.mainTexture.name);
                Assert.AreEqual(PrototypeCharacterFactory.FemaleSurvivorRearReferenceTextureName, leaderRearReferenceRenderer.sharedMaterial.mainTexture.name);
                Assert.AreNotSame(leaderReferenceRenderer.sharedMaterial.mainTexture, leaderRearReferenceRenderer.sharedMaterial.mainTexture);
                Texture2D leaderFrontReferenceTexture = leaderReferenceRenderer.sharedMaterial.mainTexture as Texture2D;
                Texture2D leaderRearReferenceTexture = leaderRearReferenceRenderer.sharedMaterial.mainTexture as Texture2D;
                Assert.IsNotNull(leaderFrontReferenceTexture);
                Assert.IsNotNull(leaderRearReferenceTexture);
                Assert.IsTrue(leaderFrontReferenceTexture.isReadable);
                Assert.AreEqual(leaderFrontReferenceTexture.width, leaderRearReferenceTexture.width);
                Assert.AreEqual(leaderFrontReferenceTexture.height, leaderRearReferenceTexture.height);
                CollectionAssert.Contains(leaderReferenceRenderer.bones, player.transform.Find("Survivor Leader"));
                CollectionAssert.Contains(leaderReferenceRenderer.bones, player.transform.Find("Survivor Leader/Human Leg Left"));
                CollectionAssert.Contains(leaderReferenceRenderer.bones, player.transform.Find("Survivor Leader/Human Leg Right"));
                CollectionAssert.Contains(leaderReferenceRenderer.bones, player.transform.Find("Survivor Leader/Human Leg Left/Human Knee Left"));
                CollectionAssert.Contains(leaderReferenceRenderer.bones, player.transform.Find("Survivor Leader/Human Leg Right/Human Knee Right"));
                CollectionAssert.Contains(leaderReferenceRenderer.bones, player.transform.Find("Survivor Leader/Human Leg Left/Human Knee Left/Human Shin Left/Human Boot Left"));
                CollectionAssert.Contains(leaderReferenceRenderer.bones, player.transform.Find("Survivor Leader/Human Leg Right/Human Knee Right/Human Shin Right/Human Boot Right"));
                CollectionAssert.Contains(leaderRearReferenceRenderer.bones, player.transform.Find("Survivor Leader"));
                CollectionAssert.Contains(leaderRearReferenceRenderer.bones, player.transform.Find("Survivor Leader/Human Leg Left/Human Knee Left"));
                CollectionAssert.Contains(leaderRearReferenceRenderer.bones, player.transform.Find("Survivor Leader/Human Leg Right/Human Knee Right"));
                Assert.AreNotSame(leaderReferenceRenderer.sharedMesh, leaderRearReferenceRenderer.sharedMesh);
                Assert.AreEqual(0f, leaderReferenceRenderer.sharedMesh.uv[0].x, 0.001f);
                Assert.AreEqual(0f, leaderReferenceRenderer.sharedMesh.uv[0].y, 0.001f);
                Assert.AreEqual(1f, leaderReferenceRenderer.sharedMesh.uv[^1].x, 0.001f);
                Assert.AreEqual(1f, leaderReferenceRenderer.sharedMesh.uv[^1].y, 0.001f);
                Assert.AreEqual(0f, leaderRearReferenceRenderer.sharedMesh.uv[0].x, 0.001f);
                Assert.AreEqual(0f, leaderRearReferenceRenderer.sharedMesh.uv[0].y, 0.001f);
                Assert.AreEqual(1f, leaderRearReferenceRenderer.sharedMesh.uv[^1].x, 0.001f);
                Assert.AreEqual(1f, leaderRearReferenceRenderer.sharedMesh.uv[^1].y, 0.001f);
                Assert.Greater(leaderReferenceRenderer.sharedMesh.bounds.size.x, 0.55f);
                Assert.Less(leaderReferenceRenderer.sharedMesh.bounds.size.z, 0.02f);
                Assert.Greater(leaderReferenceRenderer.sharedMesh.normals[0].z, 0.90f);
                Assert.Greater(leaderReferenceRenderer.sharedMesh.bounds.center.z, 0f);
                Assert.Less(leaderRearReferenceRenderer.sharedMesh.bounds.center.z, 0f);
                Assert.Less(leaderRearReferenceRenderer.sharedMesh.normals[0].z, -0.90f);
                Assert.Greater(leaderRearReferenceRenderer.sharedMesh.bounds.size.x, 0.55f);

                // Hidden skeleton materials still keep the approved teal/gray/cyan palette if re-enabled for debugging.
                Color glowColor = ReadMaterialColor(leaderChestGlowRenderer.sharedMaterial);
                Color armorColor = ReadMaterialColor(leaderChestArmorRenderer.sharedMaterial);
                Assert.Greater(glowColor.g, 0.70f);
                Assert.Greater(glowColor.b, 0.80f);
                Assert.Less(glowColor.r, 0.10f);
                Assert.Greater(armorColor.r, 0.20f);
                Assert.Greater(armorColor.g, 0.20f);
                Assert.Greater(armorColor.b, 0.20f);

                // All three visible soldier decals must remain comparable to the generated zombie body height.
                Bounds leaderBounds = CalculateEnabledRendererBounds(player.transform.Find("Survivor Leader"));
                Bounds leftBounds = CalculateEnabledRendererBounds(player.transform.Find("Survivor Left Wing"));
                Bounds rightBounds = CalculateEnabledRendererBounds(player.transform.Find("Survivor Right Wing"));
                Bounds zombieBounds = CalculateEnabledRendererBounds(zombie.transform.Find("Zombie Figure"));
                Assert.GreaterOrEqual(leaderBounds.size.y, zombieBounds.size.y * 0.85f);
                Assert.LessOrEqual(leaderBounds.size.y, zombieBounds.size.y * 1.15f);
                Assert.GreaterOrEqual(leaderBounds.size.y, leftBounds.size.y * 0.85f);
                Assert.LessOrEqual(leaderBounds.size.y, leftBounds.size.y * 1.15f);
                Assert.GreaterOrEqual(leaderBounds.size.y, rightBounds.size.y * 0.85f);
                Assert.LessOrEqual(leaderBounds.size.y, rightBounds.size.y * 1.15f);
                Assert.Greater(leaderBounds.size.x, 0.30f);
                Assert.Greater(leftBounds.size.x, 0.30f);
                Assert.Greater(rightBounds.size.x, 0.30f);

                // Every soldier should own the same rifle profile with a direct muzzle anchor at its barrel tip.
                AssertWeaponMuzzle(player.transform, "Survivor Leader", PrototypeCharacterFactory.LeaderRifleName);
                AssertWeaponMuzzle(player.transform, "Survivor Left Wing", PrototypeCharacterFactory.LeaderRifleName);
                AssertWeaponMuzzle(player.transform, "Survivor Right Wing", PrototypeCharacterFactory.LeaderRifleName);

                // All rifles should read as shoulder/eye-level firing poses.
                AssertEyeLevelWeaponHold(player.transform, "Survivor Leader", PrototypeCharacterFactory.LeaderRifleName);
                AssertEyeLevelWeaponHold(player.transform, "Survivor Left Wing", PrototypeCharacterFactory.LeaderRifleName);
                AssertEyeLevelWeaponHold(player.transform, "Survivor Right Wing", PrototypeCharacterFactory.LeaderRifleName);

                // Basic zombies need a head, face, limbs, and wound instead of a single card mesh.
                Assert.IsNull(zombie.GetComponent<MeshFilter>());
                Assert.IsNotNull(zombie.transform.Find("Zombie Figure/Zombie Head"));
                Assert.IsNotNull(zombie.transform.Find("Zombie Figure/Zombie Eye Left"));
                Assert.IsNotNull(zombie.transform.Find("Zombie Figure/Zombie Arm Right"));
                Assert.IsNotNull(zombie.transform.Find("Zombie Figure/Zombie Leg Left/Zombie Knee Left"));
                Assert.IsNotNull(zombie.transform.Find("Zombie Figure/Zombie Leg Left/Zombie Knee Left/Zombie Shin Left"));
                Assert.IsNotNull(zombie.transform.Find("Zombie Figure/Zombie Wound"));

                // Zombie thighs must contrast against the dark road so leg motion stays readable in simulator captures.
                Transform zombieThigh = zombie.transform.Find("Zombie Figure/Zombie Leg Left/Zombie Thigh Left Mesh");
                Assert.IsNotNull(zombieThigh);

                // The generated thigh mesh owns the pants material that appears on all connected zombie leg segments.
                MeshRenderer zombieThighRenderer = zombieThigh.GetComponent<MeshRenderer>();
                Assert.IsNotNull(zombieThighRenderer);

                // The test uses the same authored track color as runtime/editor scene builders.
                Color zombiePantsColor = zombieThighRenderer.sharedMaterial.color;
                Color trackColor = new(0.20f, 0.24f, 0.22f);

                // Relative luminance catches colors that are technically different but visually merge at phone scale.
                float zombiePantsLuminance = zombiePantsColor.r * 0.2126f + zombiePantsColor.g * 0.7152f + zombiePantsColor.b * 0.0722f;
                float trackLuminance = trackColor.r * 0.2126f + trackColor.g * 0.7152f + trackColor.b * 0.0722f;

                // A strong gap keeps the legs readable through GIF compression and the chase-camera perspective.
                Assert.Greater(zombiePantsLuminance - trackLuminance, 0.32f);

                // Armored zombies need explicit armor pieces so their damage reduction has a visual tell.
                Assert.IsNotNull(armoredZombie.transform.Find("Zombie Figure/Zombie Armor Plate"));
                Assert.IsNotNull(armoredZombie.transform.Find("Zombie Figure/Zombie Helmet"));

                // All generated character geometry stays collider-free.
                AssertNoColliderComponents(player);
                AssertNoColliderComponents(zombie);
                AssertNoColliderComponents(armoredZombie);
            }
            finally
            {
                // Destroy generated Unity objects explicitly so EditMode tests stay isolated.
                UnityEngine.Object.DestroyImmediate(player);
                UnityEngine.Object.DestroyImmediate(zombie);
                UnityEngine.Object.DestroyImmediate(armoredZombie);
                UnityEngine.Object.DestroyImmediate(playerMaterial);
                UnityEngine.Object.DestroyImmediate(accentMaterial);
                UnityEngine.Object.DestroyImmediate(zombieMaterial);
            }
        }

        [Test]
        public void PrototypeCharacterFactory_BuildsImportedFemaleSoldierZombiesWithInfectedDetailsAndStumble()
        {
            // Construct both enemy types so the trial covers the exposed basic face and the armored visual tell.
            Material zombieMaterial = PrototypeMaterialFactory.Create(Color.green);
            GameObject basicZombie = PrototypeCharacterFactory.CreateZombie(
                "Imported Basic Zombie Under Test",
                new Vector3(0f, GameplayVisuals.ZombieCenterY, 13f),
                zombieMaterial,
                ZombieEnemyType.Basic);
            GameObject armoredZombie = PrototypeCharacterFactory.CreateZombie(
                "Imported Armored Zombie Under Test",
                new Vector3(2f, GameplayVisuals.ZombieCenterY, 19f),
                zombieMaterial,
                ZombieEnemyType.Armored);

            try
            {
                // The visible enemy must be the licensed female SWAT mesh rather than the retained primitive fallback.
                Transform basicFigure = basicZombie.transform.Find("Zombie Figure");
                Transform basicModel = basicFigure?.Find(PrototypeCharacterFactory.SwatZombieModelName);
                Transform armoredModel = armoredZombie.transform.Find($"Zombie Figure/{PrototypeCharacterFactory.SwatZombieModelName}");
                Assert.IsNotNull(basicFigure);
                Assert.IsNotNull(basicModel);
                Assert.IsNotNull(armoredModel);

                // A Humanoid Animator drives the authored Mixamo step before the dedicated component adds instability.
                Animator basicAnimator = basicModel.GetComponent<Animator>();
                Animator armoredAnimator = armoredModel.GetComponent<Animator>();
                SwatZombieAnimator basicStumble = basicModel.GetComponent<SwatZombieAnimator>();
                SwatZombieAnimator armoredStumble = armoredModel.GetComponent<SwatZombieAnimator>();
                Assert.IsNotNull(basicAnimator);
                Assert.IsNotNull(armoredAnimator);
                Assert.IsInstanceOf<AnimatorOverrideController>(basicAnimator.runtimeAnimatorController);
                Assert.IsFalse(basicAnimator.applyRootMotion);
                Assert.AreEqual(AnimatorCullingMode.AlwaysAnimate, basicAnimator.cullingMode);
                Assert.AreEqual(SwatZombieAnimator.BasicAnimatorSpeed, basicAnimator.speed, 0.001f);
                Assert.AreEqual(SwatZombieAnimator.ArmoredAnimatorSpeed, armoredAnimator.speed, 0.001f);
                Assert.IsNotNull(basicStumble);
                Assert.IsNotNull(armoredStumble);
                Assert.IsTrue(basicStumble.HasCompleteHumanoidRig);
                Assert.AreEqual(ZombieEnemyType.Basic, basicStumble.EnemyType);
                Assert.AreEqual(ZombieEnemyType.Armored, armoredStumble.EnemyType);

                // Exaggerated bloodshot eyes, running blood, open-jaw drool, and body wounds must all follow live bones.
                Transform leftEye = FindNamedDescendant(basicModel, PrototypeCharacterFactory.SwatZombieBloodyEyeLeftName);
                Transform rightEye = FindNamedDescendant(basicModel, PrototypeCharacterFactory.SwatZombieBloodyEyeRightName);
                Transform droolStrand = FindNamedDescendant(basicModel, PrototypeCharacterFactory.SwatZombieDroolStrandName);
                Transform droolDrop = FindNamedDescendant(basicModel, PrototypeCharacterFactory.SwatZombieDroolDropName);
                Assert.IsNotNull(leftEye);
                Assert.IsNotNull(rightEye);
                Assert.IsNotNull(FindNamedDescendant(basicModel, "Zombie Bloody Socket Left"));
                Assert.IsNotNull(FindNamedDescendant(basicModel, "Zombie Bloody Socket Right"));
                Assert.IsNotNull(FindNamedDescendant(basicModel, "Zombie Eye Blood Trail Left"));
                Assert.IsNotNull(FindNamedDescendant(basicModel, "Zombie Eye Blood Trail Right"));
                Assert.IsNotNull(FindNamedDescendant(basicModel, "Zombie Pupil Left"));
                Assert.IsNotNull(FindNamedDescendant(basicModel, "Zombie Pupil Right"));
                Assert.IsNotNull(droolStrand);
                Assert.IsNotNull(droolDrop);
                Assert.IsNotNull(FindNamedDescendant(basicModel, PrototypeCharacterFactory.SwatZombieChestWoundName));
                Assert.IsNotNull(FindNamedDescendant(basicModel, PrototypeCharacterFactory.SwatZombieHeadWoundName));
                Assert.GreaterOrEqual(leftEye.lossyScale.x, 0.032f);
                Assert.GreaterOrEqual(rightEye.lossyScale.x, 0.032f);
                Assert.Greater(droolStrand.lossyScale.y, droolStrand.lossyScale.x * 5f);

                // The zombie palette must retain the source fabric texture and normal detail beneath its sickly tint.
                Renderer basicSuitRenderer = FindNamedDescendant(basicModel, "Suit")?.GetComponent<Renderer>();
                Assert.IsNotNull(basicSuitRenderer);
                Assert.That(basicSuitRenderer.sharedMaterial.name, Does.StartWith("SWAT Zombie"));
                Assert.IsNotNull(basicSuitRenderer.sharedMaterial.mainTexture);
                Assert.AreEqual("Outfit_Burglar2_Diffuse", basicSuitRenderer.sharedMaterial.mainTexture.name);
                Assert.IsNotNull(basicSuitRenderer.sharedMaterial.GetTexture("_BumpMap"));

                // Repeated zombie instances must reuse immutable runtime materials instead of leaking one set per enemy.
                Renderer armoredSuitRenderer = FindNamedDescendant(armoredModel, "Suit")?.GetComponent<Renderer>();
                Assert.IsNotNull(armoredSuitRenderer);
                Assert.AreSame(basicSuitRenderer.sharedMaterial, armoredSuitRenderer.sharedMaterial);

                // All source firearms stay hidden, the face remains exposed, and only armored enemies retain a helmet.
                foreach (Renderer renderer in basicModel.GetComponentsInChildren<Renderer>(true))
                {
                    bool isSourceWeapon = renderer.gameObject.name.StartsWith("SKM_WP_", System.StringComparison.Ordinal) ||
                                          renderer.gameObject.name.StartsWith("SM_WP_", System.StringComparison.Ordinal);
                    if (isSourceWeapon)
                    {
                        Assert.IsFalse(renderer.enabled, $"{renderer.name} should stay hidden on an unarmed zombie.");
                    }
                }

                Renderer basicMask = FindNamedDescendant(basicModel, "Balaclava_Mask")?.GetComponent<Renderer>();
                Renderer basicHelmet = FindNamedDescendant(basicModel, "AUG3M_Helmet_33393_Shape")?.GetComponent<Renderer>();
                Renderer armoredHelmet = FindNamedDescendant(armoredModel, "AUG3M_Helmet_33393_Shape")?.GetComponent<Renderer>();
                Assert.IsNotNull(basicMask);
                Assert.IsNotNull(basicHelmet);
                Assert.IsNotNull(armoredHelmet);
                Assert.IsFalse(basicMask.enabled);
                Assert.IsFalse(basicHelmet.enabled);
                Assert.IsTrue(armoredHelmet.enabled);

                // Primitive fallback geometry remains structurally available but contributes no doubled visible silhouette.
                MeshRenderer generatedHeadRenderer = basicZombie.transform.Find("Zombie Figure/Zombie Head")?.GetComponent<MeshRenderer>();
                Assert.IsNotNull(generatedHeadRenderer);
                Assert.IsFalse(generatedHeadRenderer.enabled);

                // Explicit evaluation must leave gameplay targeting fixed while changing root balance and both arms unequally.
                Vector3 gameplayRootPosition = basicZombie.transform.position;
                Vector3 figurePositionBefore = basicFigure.localPosition;
                Quaternion figureRotationBefore = basicFigure.localRotation;
                Transform leftUpperArm = basicAnimator.GetBoneTransform(HumanBodyBones.LeftUpperArm);
                Transform rightUpperArm = basicAnimator.GetBoneTransform(HumanBodyBones.RightUpperArm);
                Quaternion leftArmBefore = leftUpperArm.rotation;
                Quaternion rightArmBefore = rightUpperArm.rotation;
                Transform leftFoot = basicAnimator.GetBoneTransform(HumanBodyBones.LeftFoot);
                Transform rightFoot = basicAnimator.GetBoneTransform(HumanBodyBones.RightFoot);
                float expectedGroundHeight = Mathf.Min(
                    basicZombie.transform.InverseTransformPoint(leftFoot.position).y,
                    basicZombie.transform.InverseTransformPoint(rightFoot.position).y);
                basicStumble.ForceEvaluate(0.37f);
                Assert.IsTrue(basicStumble.IsAnimating);
                Assert.Greater(basicStumble.AnimationPhase, 0f);
                Assert.AreEqual(gameplayRootPosition, basicZombie.transform.position);
                Assert.Greater(Vector3.Distance(figurePositionBefore, basicFigure.localPosition), 0.001f);
                Assert.Greater(Quaternion.Angle(figureRotationBefore, basicFigure.localRotation), 1f);
                Assert.Greater(Quaternion.Angle(leftArmBefore, leftUpperArm.rotation), 2f);
                Assert.Greater(Quaternion.Angle(rightArmBefore, rightUpperArm.rotation), 2f);
                Assert.Greater(
                    Mathf.Abs(basicStumble.CurrentLeftArmAgitationDegrees - basicStumble.CurrentRightArmAgitationDegrees),
                    0.5f);

                // Sample a complete unstable cycle and require its current lower support boot to stay on the road plane.
                for (int sampleIndex = 0; sampleIndex < 120; sampleIndex++)
                {
                    basicStumble.ForceEvaluate(1f / 30f);
                    float sampledGroundHeight = Mathf.Min(
                        basicZombie.transform.InverseTransformPoint(leftFoot.position).y,
                        basicZombie.transform.InverseTransformPoint(rightFoot.position).y);
                    Assert.AreEqual(expectedGroundHeight, sampledGroundHeight, 0.002f);
                    Assert.AreEqual(0f, basicStumble.CurrentSupportFootGroundError, 0.002f);
                }

                // Imported and generated render-only visual layers must remain free of accidental physics colliders.
                AssertNoColliderComponents(basicZombie);
                AssertNoColliderComponents(armoredZombie);
            }
            finally
            {
                // Destroy every generated object and the caller-owned fallback material to isolate later EditMode tests.
                UnityEngine.Object.DestroyImmediate(basicZombie);
                UnityEngine.Object.DestroyImmediate(armoredZombie);
                UnityEngine.Object.DestroyImmediate(zombieMaterial);
            }
        }

        [Test]
        public void PrototypeHumanoidAnimator_MovesSurvivorAndZombieLimbs()
        {
            // The procedural walk cycle should visibly rotate generated limbs without imported animation clips.
            Material playerMaterial = PrototypeMaterialFactory.Create(Color.cyan);
            Material accentMaterial = PrototypeMaterialFactory.Create(Color.magenta);
            Material zombieMaterial = PrototypeMaterialFactory.Create(Color.green);
            GameObject player = PrototypeCharacterFactory.CreatePlayerSquad("Animated Player Under Test", Vector3.zero, playerMaterial, accentMaterial);
            GameObject zombie = PrototypeCharacterFactory.CreateZombie("Animated Zombie Under Test", Vector3.forward, zombieMaterial, ZombieEnemyType.Basic);

            try
            {
                // The player animator should cache all three generated survivor rigs.
                PrototypeHumanoidAnimator playerAnimator = player.GetComponent<PrototypeHumanoidAnimator>();
                Assert.IsNotNull(playerAnimator);
                Assert.AreEqual(PrototypeHumanoidAnimationStyle.SurvivorSquad, playerAnimator.AnimationStyle);
                Assert.AreEqual(3, playerAnimator.AnimatedRigCount);

                // The imported leader owns an authored Animator while the wing survivors retain the procedural rig.
                Transform playerRoot = player.transform.Find("Survivor Leader");
                Transform playerSwatModel = playerRoot.Find(PrototypeCharacterFactory.SwatSurvivorModelName);
                Animator playerSwatAnimator = playerSwatModel.GetComponent<Animator>();
                SwatSurvivorLocomotionAnimator playerSwatLocomotion = playerSwatModel.GetComponent<SwatSurvivorLocomotionAnimator>();
                Transform importedPlayerLeg = FindNamedDescendant(playerSwatModel, "LeftUpperLeg");
                Transform proceduralPlayerRoot = player.transform.Find("Survivor Left Wing");
                Transform playerLeg = proceduralPlayerRoot.Find("Human Leg Left");
                Transform playerKnee = proceduralPlayerRoot.Find("Human Leg Left/Human Knee Left");
                Transform playerFoot = proceduralPlayerRoot.Find("Human Leg Left/Human Knee Left/Human Shin Left/Human Boot Left");
                Transform playerWeaponArm = proceduralPlayerRoot.Find("Human Arm Right");
                Transform playerWeaponHand = proceduralPlayerRoot.Find("Human Arm Right/Human Hand Right");
                Transform playerWeapon = player.transform.Find($"Survivor Leader/Human Arm Right/Human Hand Right/{PrototypeCharacterFactory.LeaderRifleName}");
                Transform playerMuzzle = FindNamedDescendant(playerSwatModel, PlayerSquad.WeaponMuzzleAnchorName);
                Transform visibleWeaponAimPivot = FindNamedDescendant(playerSwatModel, PrototypeCharacterFactory.SwatWeaponAimPivotName);
                SkinnedMeshRenderer visibleMuzzleRenderer = FindNamedDescendant(playerSwatModel, PrototypeCharacterFactory.SwatWeaponMuzzleRendererName).GetComponent<SkinnedMeshRenderer>();
                SkinnedMeshRenderer visibleStockRenderer = FindNamedDescendant(playerSwatModel, PrototypeCharacterFactory.SwatWeaponStockRendererName).GetComponent<SkinnedMeshRenderer>();
                Transform playerReferenceUpper = player.transform.Find($"Survivor Leader/{PrototypeCharacterFactory.FemaleReferenceUpperName}");
                SkinnedMeshRenderer playerReferenceRenderer = playerReferenceUpper.GetComponent<SkinnedMeshRenderer>();
                Assert.IsNotNull(playerRoot);
                Assert.IsNotNull(playerSwatModel);
                Assert.IsNotNull(playerSwatAnimator);
                Assert.IsNotNull(playerSwatAnimator.runtimeAnimatorController);
                Assert.IsFalse(playerSwatAnimator.applyRootMotion);
                Assert.IsNotNull(playerSwatLocomotion);
                Assert.IsNotNull(importedPlayerLeg);
                Assert.IsNotNull(proceduralPlayerRoot);
                Assert.IsNotNull(playerLeg);
                Assert.IsNotNull(playerKnee);
                Assert.IsNotNull(playerFoot);
                Assert.IsNotNull(playerWeaponArm);
                Assert.IsNotNull(playerWeaponHand);
                Assert.IsNotNull(playerWeapon);
                Assert.IsNotNull(playerMuzzle);
                Assert.IsNotNull(visibleWeaponAimPivot);
                Assert.IsNotNull(visibleMuzzleRenderer);
                Assert.IsNotNull(visibleStockRenderer);

                // The unreliable imported weapon skins stay hidden behind rigid pivot-local render proxies.
                Assert.IsFalse(visibleMuzzleRenderer.enabled);
                Assert.IsFalse(visibleStockRenderer.enabled);
                int rigidWeaponRendererCount = 0;
                foreach (MeshRenderer weaponRenderer in visibleWeaponAimPivot.GetComponentsInChildren<MeshRenderer>(true))
                {
                    if (weaponRenderer.gameObject.name.EndsWith(PrototypeCharacterFactory.SwatWeaponRigidRendererSuffix, System.StringComparison.Ordinal))
                    {
                        Assert.IsTrue(weaponRenderer.enabled);
                        rigidWeaponRendererCount++;
                    }
                }

                Assert.Greater(rigidWeaponRendererCount, 0);
                Assert.IsNotNull(playerReferenceUpper);
                Assert.IsNotNull(playerReferenceRenderer);
                Assert.IsFalse(playerReferenceRenderer.enabled);
                Assert.IsNull(playerRoot.Find(PrototypeCharacterFactory.SoldierReferenceVisualName));
                Vector3 playerRootRestLocalPosition = playerRoot.localPosition;
                Quaternion playerRootRestRotation = playerRoot.localRotation;
                Quaternion importedPlayerLegRestRotation = importedPlayerLeg.localRotation;
                Quaternion playerLegRestRotation = playerLeg.localRotation;
                Quaternion playerKneeRestRotation = playerKnee.localRotation;
                Quaternion playerFootRestRotation = playerFoot.localRotation;
                Quaternion playerWeaponArmRestRotation = playerWeaponArm.localRotation;
                Quaternion playerWeaponHandRestRotation = playerWeaponHand.localRotation;
                Quaternion playerWeaponRestRotation = playerWeapon.localRotation;

                // A forced moving evaluation drives the procedural wing rigs without moving the authored leader parent.
                playerAnimator.ForceEvaluate(0.4f, true);
                Assert.IsTrue(playerAnimator.IsAnimating);
                float playerThighSwing = Quaternion.Angle(playerLegRestRotation, playerLeg.localRotation);
                float playerKneeBend = Quaternion.Angle(playerKneeRestRotation, playerKnee.localRotation);
                Assert.Greater(playerKneeBend, 5f, $"Wing procedural knee should bend; thigh swing was {playerThighSwing:F2} degrees.");
                Assert.Greater(Quaternion.Angle(playerFootRestRotation, playerFoot.localRotation), 0.1f, "Wing procedural foot should pitch during a forced run step.");
                Assert.Less(Vector3.Distance(playerRootRestLocalPosition, playerRoot.localPosition), 0.001f, "Procedural locomotion must not shift the Mixamo leader parent.");
                Assert.Less(Quaternion.Angle(playerRootRestRotation, playerRoot.localRotation), 0.001f, "Procedural locomotion must not rotate the Mixamo leader parent.");
                Quaternion playerRootRunningRotation = playerRoot.localRotation;

                // The procedural evaluator must not overwrite the imported leader bones owned by its Animator.
                Assert.Less(Quaternion.Angle(importedPlayerLegRestRotation, importedPlayerLeg.localRotation), 0.001f);

                // Procedural wing firing arms continue to pump while the authored leader animation remains independent.
                Assert.Greater(Quaternion.Angle(playerWeaponArmRestRotation, playerWeaponArm.localRotation), 0.1f);
                Assert.Greater(Quaternion.Angle(playerWeaponHandRestRotation, playerWeaponHand.localRotation), 0.1f);
                Assert.Less(Quaternion.Angle(playerWeaponRestRotation, playerWeapon.localRotation), 0.001f);

                // A direct shot rotates only the imported rigid weapon hierarchy while leaving locomotion bones untouched.
                Vector3 playerWeaponRunningLocalPosition = playerWeapon.localPosition;
                Quaternion playerWeaponRunningRotation = playerWeapon.localRotation;
                Quaternion visibleWeaponRunningRotation = visibleWeaponAimPivot.rotation;
                Quaternion playerRootRunningBeforeShotRotation = playerRoot.localRotation;
                Vector3 visibleWeaponTarget = playerMuzzle.position + new Vector3(1.6f, -0.1f, 3f);
                Transform visibleStockBone = FindNamedDescendant(playerSwatModel, PrototypeCharacterFactory.SwatWeaponStockBoneName);
                Assert.IsNotNull(visibleStockBone);
                Assert.IsTrue(playerSwatLocomotion.PlayWeaponShot(playerMuzzle, visibleWeaponTarget));
                playerAnimator.ForceEvaluate(0.02f, true);
                Assert.Less(Vector3.Distance(playerWeaponRunningLocalPosition, playerWeapon.localPosition), 0.001f);
                Assert.Less(Quaternion.Angle(playerWeaponRunningRotation, playerWeapon.localRotation), 0.001f);
                Assert.Less(Quaternion.Angle(playerWeaponRestRotation, playerWeapon.localRotation), 0.001f);
                Assert.IsNotNull(FindNamedDescendant(playerSwatModel, PrototypeCharacterFactory.SwatWeaponBarrelBaseName));
                Assert.Greater(Quaternion.Angle(visibleWeaponRunningRotation, visibleWeaponAimPivot.rotation), 0.1f);
                Assert.LessOrEqual(
                    Vector3.Angle(playerMuzzle.position - visibleStockBone.position, visibleWeaponTarget - playerMuzzle.position),
                    SwatSurvivorLocomotionAnimator.MaximumWeaponAimErrorDegrees,
                    "The imported stock-to-muzzle axis must point directly at the zombie.");
                Assert.Less(Quaternion.Angle(playerRootRunningBeforeShotRotation, playerRoot.localRotation), 0.001f);
                Assert.Less(Quaternion.Angle(playerRootRunningRotation, playerRoot.localRotation), 0.001f);
                Assert.Less(Quaternion.Angle(importedPlayerLegRestRotation, importedPlayerLeg.localRotation), 0.001f);

                // The zombie animator should shamble even when the gameplay root is stationary.
                PrototypeHumanoidAnimator zombieAnimator = zombie.GetComponent<PrototypeHumanoidAnimator>();
                Assert.IsNotNull(zombieAnimator);
                Assert.AreEqual(PrototypeHumanoidAnimationStyle.ZombieShamble, zombieAnimator.AnimationStyle);
                Assert.AreEqual(1, zombieAnimator.AnimatedRigCount);

                // Capture rest-pose zombie arm and knee joints before forcing an in-place shamble step.
                Transform zombieArm = zombie.transform.Find("Zombie Figure/Zombie Arm Left");
                Transform zombieKnee = zombie.transform.Find("Zombie Figure/Zombie Leg Left/Zombie Knee Left");
                Transform zombieShin = zombie.transform.Find("Zombie Figure/Zombie Leg Left/Zombie Knee Left/Zombie Shin Left");
                Assert.IsNotNull(zombieArm);
                Assert.IsNotNull(zombieKnee);
                Assert.IsNotNull(zombieShin);
                Assert.AreSame(zombieKnee, zombieShin.parent);
                Quaternion zombieArmRestRotation = zombieArm.localRotation;
                Quaternion zombieKneeRestRotation = zombieKnee.localRotation;

                // Stationary zombies should still animate because they are waiting threats, not moving agents.
                zombieAnimator.ForceEvaluate(1f, false);
                Assert.IsTrue(zombieAnimator.IsAnimating);
                Assert.Greater(Quaternion.Angle(zombieArmRestRotation, zombieArm.localRotation), 0.1f);
                Assert.Greater(Quaternion.Angle(zombieKneeRestRotation, zombieKnee.localRotation), 0.1f);
                Assert.Less(Vector3.Distance(zombieKnee.position, zombieShin.position), 0.001f);
            }
            finally
            {
                // Destroy generated Unity objects explicitly so EditMode tests stay isolated.
                UnityEngine.Object.DestroyImmediate(player);
                UnityEngine.Object.DestroyImmediate(zombie);
                UnityEngine.Object.DestroyImmediate(playerMaterial);
                UnityEngine.Object.DestroyImmediate(accentMaterial);
                UnityEngine.Object.DestroyImmediate(zombieMaterial);
            }
        }

        [Test]
        public void GameplayVisuals_KeepActorsAboveTrackSurface()
        {
            // The squad bottom must stay visibly above the generated track surface.
            Assert.Greater(GameplayVisuals.PlayerCenterY - GameplayVisuals.PlayerHeight * 0.5f, GameplayVisuals.TrackTopY);

            // Humanoid feet should be close enough to the track to look grounded instead of floating.
            Assert.LessOrEqual(GameplayVisuals.PlayerCenterY - GameplayVisuals.PlayerHeight * 0.5f - GameplayVisuals.TrackTopY, 0.5f);

            // Gate roots stay at road height because gameplay contact still happens at lane center.
            Assert.AreEqual(GameplayVisuals.TrackTopY, GameplayVisuals.GateRootY);

            // Low gate footprints are disabled because they appeared as gate pieces rising from underground.
            Assert.IsFalse(GameplayVisuals.GateFootprintEnabled);

            // Flat gate footprints should sit above the track surface if they are re-enabled later.
            Assert.Greater(GameplayVisuals.GateFootprintY, GameplayVisuals.TrackTopY);

            // The footprint should be a decal-height marker, not a raised curb with visible side faces.
            Assert.LessOrEqual(GameplayVisuals.GateFootprintY - GameplayVisuals.TrackTopY, 0.05f);

            // The footprint is decorative; the main gate card is the stable always-visible marker.
            Assert.LessOrEqual(GameplayVisuals.GateFootprintWidth, GameplayVisuals.GateCardWidth);

            // Footprints should remain narrow enough that neighboring lanes do not visually merge.
            Assert.LessOrEqual(GameplayVisuals.GateFootprintWidth, GameplayVisuals.SideLaneX);

            // The finish marker should also be flat so its side edge cannot appear as a green obstacle.
            Assert.Greater(GameplayVisuals.FinishLineY, GameplayVisuals.TrackTopY);

            // The full gate card must sit high enough that its bottom is not hidden by the road horizon.
            Assert.GreaterOrEqual(GameplayVisuals.GateCardBottomY - GameplayVisuals.TrackTopY, 1.1f);

            // Gate cards should stay compact so they read as lane markers instead of walls.
            Assert.LessOrEqual(GameplayVisuals.GateCardHeight, 0.8f);

            // The card center is derived from bottom and height so it cannot drift toward the road.
            float expectedGateCardCenter = GameplayVisuals.GateCardBottomY + GameplayVisuals.GateCardHeight * 0.5f;
            Assert.AreEqual(expectedGateCardCenter, GameplayVisuals.GateCardCenterY, 0.001f);

            // Zombie visual bounds start just above the road so enemies look like bodies standing in lanes.
            Assert.GreaterOrEqual(GameplayVisuals.ZombieCardBottomY - GameplayVisuals.TrackTopY, 0.08f);

            // Zombie feet should stay near the surface instead of floating like the previous high cards.
            float zombieClearance = GameplayVisuals.ZombieCenterY - GameplayVisuals.ZombieHeight * 0.5f - GameplayVisuals.TrackTopY;
            Assert.GreaterOrEqual(zombieClearance, 0.08f);
            Assert.LessOrEqual(zombieClearance, 0.16f);

            // Humanoid zombies should be taller than gate labels while still staying compact for portrait framing.
            Assert.Greater(GameplayVisuals.ZombieHeight, GameplayVisuals.GateCardHeight);
            Assert.LessOrEqual(GameplayVisuals.ZombieHeight, 1.4f);

            // World labels should sit above actor bases so finish and gate context remains readable.
            Assert.Greater(GameplayVisuals.WorldLabelY, GameplayVisuals.TrackTopY + 1f);

            // Floating feedback starts high enough that the road horizon cannot bury combat labels.
            Assert.GreaterOrEqual(GameplayVisuals.ShotTracerMinimumY, GameplayVisuals.TrackTopY + 1.2f);

            // Tracers must stay narrow compared with actors and gate cards so they cannot read as obstacles.
            Assert.Less(GameplayVisuals.ShotTracerWidth, GameplayVisuals.PlayerFootprint * 0.3f);
            Assert.Less(GameplayVisuals.ShotTracerWidth, GameplayVisuals.GateCardWidth * 0.16f);

            // Muzzle flashes must remain larger than the tracer parameter but compact enough to avoid lane pickups.
            Assert.Greater(GameplayVisuals.MuzzleFlashSize, GameplayVisuals.ShotTracerWidth);
            Assert.Less(GameplayVisuals.MuzzleFlashSize, GameplayVisuals.PlayerFootprint * 0.45f);

            // Weapon-origin tracer streaks should extend beyond the flare without spanning from off-screen zombies.
            Assert.Greater(GameplayVisuals.ShotTracerWeaponForwardLength, GameplayVisuals.MuzzleFlashForwardLength);
            Assert.Less(GameplayVisuals.ShotTracerWeaponForwardLength, GameplayVisuals.PlayerFootprint);

            // Fallback-only flash lift should stay tiny so real weapon-origin shots remain attached to the muzzle.
            Assert.GreaterOrEqual(GameplayVisuals.MuzzleFlashLiftY, 0f);
            Assert.Less(GameplayVisuals.MuzzleFlashLiftY, GameplayVisuals.ShotTracerWidth);

            // Muzzle flashes need to survive a 12fps capture frame but not trail behind the moving squad.
            Assert.GreaterOrEqual(GameplayVisuals.MuzzleFlashLifetimeSeconds, 0.10f);
            Assert.Less(GameplayVisuals.MuzzleFlashLifetimeSeconds, GameplayVisuals.ShotTracerLifetimeSeconds);

            // The legacy lateral tracer offset should separate fallback shots from lane stripes without crossing lanes.
            Assert.Greater(GameplayVisuals.ShotTracerLaneOffsetX, GameplayVisuals.PlayerMastWidth);
            Assert.Less(GameplayVisuals.ShotTracerLaneOffsetX, GameplayVisuals.LaneMatchTolerance);

            // The legacy forward muzzle offset should clear the wider survivor formation without jumping near the target.
            Assert.Greater(GameplayVisuals.ShotTracerMuzzleForwardOffsetZ, GameplayVisuals.PlayerFootprint * 1.8f);
            Assert.Less(GameplayVisuals.ShotTracerMuzzleForwardOffsetZ, GameplayVisuals.ZombieCardWidth * 2f);

            // Feedback text should remain smaller than gate labels so it reads as a temporary event.
            Assert.LessOrEqual(GameplayVisuals.FeedbackLabelScale, 0.32f);
        }

        [Test]
        public void GameplayVisuals_KeepSideLaneGatesInsidePortraitTrack()
        {
            // Side lanes are gameplay lanes, not visual-only offsets, so they must fit the portrait framing.
            Assert.Less(GameplayVisuals.SideLaneX, 2f);

            // Side lanes stay tight enough that side gates appear before the contact moment in portrait.
            Assert.LessOrEqual(GameplayVisuals.SideLaneX, 1.2f);

            // Gate visuals should stay inside the generated road width even in side lanes.
            float sideGateOuterX = GameplayVisuals.SideLaneX + GameplayVisuals.GateCardWidth * 0.5f;
            Assert.Less(sideGateOuterX, GameplayVisuals.TrackWidth * 0.5f);

            // Lane tolerance should be below half the lane spacing so neighboring lanes remain distinct.
            Assert.Less(GameplayVisuals.LaneMatchTolerance, GameplayVisuals.SideLaneX * 0.5f);
        }

        [Test]
        public void GameplayVisuals_KeepWorldHumanoidPlayerReadable()
        {
            // The player should now be visible as world-space survivor figures instead of only a HUD marker.
            Assert.IsFalse(GameplayVisuals.UseScreenSpacePlayerMarker);

            // The generated world player meshes are the primary actor representation.
            Assert.IsTrue(GameplayVisuals.WorldPlayerMeshRenderersEnabled);

            // The fallback overlay marker should remain compact if it is re-enabled for simulator triage.
            Assert.LessOrEqual(GameplayVisuals.ScreenPlayerMarkerWidth, 40f);
            Assert.LessOrEqual(GameplayVisuals.ScreenPlayerMarkerHeight, 52f);

            // The squad body should be tall enough to read as people but still smaller than the gate skyline.
            Assert.GreaterOrEqual(GameplayVisuals.PlayerHeight, 1.2f);
            Assert.LessOrEqual(GameplayVisuals.PlayerHeight, 1.45f);

            // Legacy marker constants should stay small in case the fallback is re-enabled later.
            Assert.LessOrEqual(GameplayVisuals.PlayerMastHeight, 0.55f);

            // The high beacon fallback should stay compact so it cannot become a second player body.
            Assert.Less(GameplayVisuals.PlayerBeaconFootprint, GameplayVisuals.PlayerFootprint);

            // The generated squad top should remain below the gate top so people do not visually swallow gates.
            float playerTop = GameplayVisuals.PlayerCenterY + GameplayVisuals.PlayerHeight * 0.5f;
            float gateCardTop = GameplayVisuals.GateCardCenterY + GameplayVisuals.GateCardHeight * 0.5f;
            Assert.Less(playerTop, gateCardTop);
        }

        [Test]
        public void PlayerSquadScreenMarker_CreateRequiresRectTransformParent()
        {
            // A null parent should fail loudly instead of creating a marker that silently never updates.
            Assert.Throws<System.ArgumentNullException>(() => PlayerSquadScreenMarker.Create(null, null, null));
        }

        [Test]
        public void GameplayVisuals_KeepCameraFramingBodyReadable()
        {
            // A normal runner FOV avoids the flattened look of an overhead orthographic board.
            Assert.GreaterOrEqual(GameplayVisuals.CameraFieldOfView, 45f);
            Assert.LessOrEqual(GameplayVisuals.CameraFieldOfView, 68f);

            // The responsive cap may widen tall-phone framing, but it should still avoid fisheye distortion.
            Assert.GreaterOrEqual(GameplayVisuals.CameraMaximumFieldOfView, GameplayVisuals.CameraFieldOfView);
            Assert.LessOrEqual(GameplayVisuals.CameraMaximumFieldOfView, 76f);

            // A negative Z offset keeps the camera behind the squad rather than letting it run toward the horizon.
            Assert.Less(GameplayVisuals.CameraOffset.z, 0f);

            // A forward look bias makes the camera look down-lane instead of straight down at the player.
            Assert.Greater(GameplayVisuals.CameraLookAtOffset.z, 0f);

            // Too much forward bias pins the squad to the bottom edge and makes it disappear during feedback.
            Assert.LessOrEqual(GameplayVisuals.CameraLookAtOffset.z, 2f);

            // The forward component should remain meaningful, ruling out a 90-degree view.
            Vector3 cameraToLookAt = GameplayVisuals.CameraLookAtOffset - GameplayVisuals.CameraOffset;
            Assert.Greater(Mathf.Abs(cameraToLookAt.z), Mathf.Abs(cameraToLookAt.y) * 0.65f);

            // The camera needs enough width/depth to show side gates without looking straight down.
            float downwardAngle = Mathf.Atan2(Mathf.Abs(cameraToLookAt.y), Mathf.Abs(cameraToLookAt.z)) * Mathf.Rad2Deg;
            Assert.GreaterOrEqual(downwardAngle, 36f);
            Assert.Less(downwardAngle, 50f);
        }

        [Test]
        public void GameplayVisuals_WidensCameraFovOnlyForNarrowPortraitAspects()
        {
            // The reference aspect should keep the hand-tuned simulator framing unchanged.
            float referenceFieldOfView = GameplayVisuals.GetCameraFieldOfViewForAspect(GameplayVisuals.CameraReferenceAspect);
            Assert.AreEqual(GameplayVisuals.CameraFieldOfView, referenceFieldOfView, 0.001f);

            // Tablet and wider phone shapes already show enough lane width, so they should not zoom out further.
            float tabletPortraitFieldOfView = GameplayVisuals.GetCameraFieldOfViewForAspect(3f / 4f);
            Assert.AreEqual(GameplayVisuals.CameraFieldOfView, tabletPortraitFieldOfView, 0.001f);

            // Invalid camera aspects can occur before a render target is ready and should use the stable reference.
            float fallbackFieldOfView = GameplayVisuals.GetCameraFieldOfViewForAspect(0f);
            Assert.AreEqual(GameplayVisuals.CameraFieldOfView, fallbackFieldOfView, 0.001f);

            // Very tall portrait phones need extra vertical FOV to preserve the same horizontal lane coverage.
            float tallPhoneAspect = 9f / 21.5f;
            float tallPhoneFieldOfView = GameplayVisuals.GetCameraFieldOfViewForAspect(tallPhoneAspect);
            Assert.Greater(tallPhoneFieldOfView, GameplayVisuals.CameraFieldOfView);
            Assert.LessOrEqual(tallPhoneFieldOfView, GameplayVisuals.CameraMaximumFieldOfView);

            // The widened vertical FOV should preserve the reference horizontal angle for supported tall phones.
            float referenceHorizontalFieldOfView = CalculateHorizontalFieldOfView(referenceFieldOfView, GameplayVisuals.CameraReferenceAspect);
            float tallPhoneHorizontalFieldOfView = CalculateHorizontalFieldOfView(tallPhoneFieldOfView, tallPhoneAspect);
            Assert.AreEqual(referenceHorizontalFieldOfView, tallPhoneHorizontalFieldOfView, 0.001f);
        }

        [Test]
        public void SimpleCameraFollow_AppliesResponsiveFieldOfViewWhenAspectChanges()
        {
            GameObject cameraObject = new("Responsive Camera Under Test");
            GameObject targetObject = new("Camera Target Under Test");

            try
            {
                // A perspective camera is required because orthographic cameras ignore vertical FOV.
                Camera camera = cameraObject.AddComponent<Camera>();
                camera.orthographic = false;
                camera.aspect = GameplayVisuals.CameraReferenceAspect;
                camera.fieldOfView = 25f;

                // The follow component should correct the initial FOV during initialization.
                SimpleCameraFollow follow = cameraObject.AddComponent<SimpleCameraFollow>();
                follow.Initialize(targetObject.transform, GameplayVisuals.CameraOffset, GameplayVisuals.CameraLookAtOffset, false);
                Assert.AreEqual(GameplayVisuals.CameraFieldOfView, camera.fieldOfView, 0.001f);

                // A later aspect change simulates rotating or resizing the Game view before the next frame.
                float tallPhoneAspect = 9f / 21.5f;
                camera.aspect = tallPhoneAspect;
                InvokeSimpleCameraFollowLateUpdate(follow);

                // LateUpdate should refresh the FOV to the same value as the shared visual rule.
                float expectedTallFieldOfView = GameplayVisuals.GetCameraFieldOfViewForAspect(tallPhoneAspect);
                Assert.AreEqual(expectedTallFieldOfView, camera.fieldOfView, 0.001f);
            }
            finally
            {
                // Destroy generated Unity objects explicitly so EditMode tests stay isolated.
                UnityEngine.Object.DestroyImmediate(cameraObject);
                UnityEngine.Object.DestroyImmediate(targetObject);
            }
        }

        [Test]
        public void GameplayVisuals_WithVisualY_PreservesLaneAndDistance()
        {
            // Visual staging should not alter lane matching or level distance data.
            Vector3 visualPosition = GameplayVisuals.WithVisualY(new Vector3(-2f, 99f, 12f), GameplayVisuals.GateRootY);

            Assert.AreEqual(-2f, visualPosition.x);
            Assert.AreEqual(GameplayVisuals.GateRootY, visualPosition.y);
            Assert.AreEqual(12f, visualPosition.z);
        }

        private static PlayerSquad CreateSquad(int startingCount, float startingDamage)
        {
            GameObject squadObject = new("Squad Under Test");

            LevelDefinition levelDefinition = ScriptableObject.CreateInstance<LevelDefinition>();
            levelDefinition.startingSquadCount = startingCount;
            levelDefinition.startingDamagePerMember = startingDamage;
            levelDefinition.squadMoveSpeed = 1f;
            levelDefinition.lanePositions = new[] { -GameplayVisuals.SideLaneX, 0f, GameplayVisuals.SideLaneX };

            PlayerSquad squad = squadObject.AddComponent<PlayerSquad>();
            squad.Initialize(levelDefinition);
            return squad;
        }

        private static Gate CreateGate(GateModifierType modifierType, int squadValue, float damageValue, Vector3 position)
        {
            GameObject gateObject = new("Gate Under Test");
            GateSpawnDefinition gateDefinition = new()
            {
                modifierType = modifierType,
                squadValue = squadValue,
                damageValue = damageValue,
                position = position
            };

            Gate gate = gateObject.AddComponent<Gate>();
            gate.Configure(gateDefinition, null, null);
            return gate;
        }

        private static void AssertMaterialColor(Color expectedColor, Material material)
        {
            // Tests should fail loudly if a renderable marker loses its material.
            Assert.IsNotNull(material);

            // Read through the same shader color property path the runtime tint code writes.
            Color actualColor = ReadMaterialColor(material);

            // Floating color values are exact here in practice, but tolerances keep the test shader-agnostic.
            Assert.AreEqual(expectedColor.r, actualColor.r, 0.001f);
            Assert.AreEqual(expectedColor.g, actualColor.g, 0.001f);
            Assert.AreEqual(expectedColor.b, actualColor.b, 0.001f);
            Assert.AreEqual(expectedColor.a, actualColor.a, 0.001f);
        }

        private static void AssertWeaponMuzzle(Transform playerRoot, string survivorName, string weaponName)
        {
            // The generated profile remains under the right hand even when the imported leader replaces its shot origin.
            Transform weapon = playerRoot.Find($"{survivorName}/Human Arm Right/Human Hand Right/{weaponName}");
            Assert.IsNotNull(weapon, $"{weaponName} should exist under {survivorName}'s right hand.");

            // The SWAT leader uses its visible imported barrel; generated wing weapons retain direct child anchors.
            Transform survivor = playerRoot.Find(survivorName);
            Assert.IsNotNull(survivor);
            Transform swatModel = survivor.Find(PrototypeCharacterFactory.SwatSurvivorModelName);
            Transform muzzle = swatModel != null
                ? FindNamedDescendant(swatModel, PlayerSquad.WeaponMuzzleAnchorName)
                : weapon.Find(PlayerSquad.WeaponMuzzleAnchorName);
            Assert.IsNotNull(muzzle, $"{weaponName} should have a named muzzle anchor.");
            if (swatModel != null)
            {
                // The obsolete carrier must stay unregistered while the visible anchor shares the rigid weapon pivot.
                Assert.IsNotNull(weapon.Find(PrototypeCharacterFactory.HiddenGeneratedWeaponMuzzleName));
                Assert.AreEqual(PrototypeCharacterFactory.SwatWeaponAimPivotName, muzzle.parent.name);
            }
            else
            {
                Assert.AreSame(weapon, muzzle.parent);
            }

            // The true imported barrel tip can sit lower in its diagonal rest pose than generated eye-level anchors.
            float minimumMuzzleHeight = swatModel != null
                ? GameplayVisuals.TrackTopY + 0.30f
                : GameplayVisuals.TrackTopY + 1.0f;
            Assert.Greater(muzzle.position.y, minimumMuzzleHeight);

            // The effect muzzle should stay inside the soldier footprint instead of drifting ahead of the decal rifle.
            Assert.Greater(muzzle.position.z, survivor.position.z - 0.15f);
            Assert.Less(muzzle.position.z, survivor.position.z + GameplayVisuals.PlayerFootprint);

            if (swatModel != null)
            {
                // The imported rest pose may carry the rifle diagonally, but the runtime anchor must follow its barrel axis.
                Transform barrelBase = FindNamedDescendant(swatModel, PrototypeCharacterFactory.SwatWeaponBarrelBaseName);
                Assert.IsNotNull(barrelBase);
                Assert.Greater(
                    Vector3.Dot(muzzle.forward.normalized, (muzzle.position - barrelBase.position).normalized),
                    0.99f,
                    $"{weaponName} muzzle should follow the visible imported barrel before target aim is applied.");
            }
            else
            {
                // Generated wing muzzles use authored local +Z as their forward firing axis.
                Assert.Greater(Vector3.Dot(muzzle.forward.normalized, Vector3.forward), 0.90f, $"{weaponName} muzzle should face down-lane.");
            }
        }

        private static void AssertEyeLevelWeaponHold(Transform playerRoot, string survivorName, string weaponName)
        {
            // The survivor head provides a stable local reference for eye-level weapon placement.
            Transform head = playerRoot.Find($"{survivorName}/Human Head");
            Assert.IsNotNull(head, $"{survivorName} should have a head reference for weapon pose checks.");

            // Hidden weapon anchors must advance with the soldier so attached effects start from the decal rifle pose.
            Transform weapon = playerRoot.Find($"{survivorName}/Human Arm Right/Human Hand Right/{weaponName}");
            Assert.IsNotNull(weapon, $"{weaponName} should exist for visible weapon pose checks.");

            // Generated weapon primitives must stay hidden so they do not render as detached guns over the decals.
            foreach (MeshRenderer renderer in weapon.GetComponentsInChildren<MeshRenderer>(true))
            {
                // The approved front and generated rear soldier decals provide the visible raised rifle instead.
                Assert.IsFalse(renderer.enabled, $"{weaponName} primitive mesh should stay hidden behind {survivorName}'s decal.");
            }

            // The leader resolves the imported barrel anchor while decal wings keep their generated child muzzles.
            Transform survivor = playerRoot.Find(survivorName);
            Transform swatModel = survivor != null ? survivor.Find(PrototypeCharacterFactory.SwatSurvivorModelName) : null;
            Transform muzzle = swatModel != null
                ? FindNamedDescendant(swatModel, PlayerSquad.WeaponMuzzleAnchorName)
                : playerRoot.Find($"{survivorName}/Human Arm Right/Human Hand Right/{weaponName}/{PlayerSquad.WeaponMuzzleAnchorName}");
            Assert.IsNotNull(muzzle, $"{weaponName} should have a muzzle for weapon pose checks.");

            if (swatModel != null)
            {
                // EditMode observes the FBX rest pose before Animator evaluation; only require a safe above-road origin here.
                Assert.Greater(muzzle.position.y, GameplayVisuals.TrackTopY + 0.30f, $"{weaponName} imported muzzle should stay above the road.");
            }
            else
            {
                // Generated shoulder-fired weapons are fully authored at construction and should already sit by the face.
                Assert.GreaterOrEqual(muzzle.position.y, head.position.y - 0.08f, $"{weaponName} should be held near eye level.");
            }
        }

        private static void AssertNoColliderComponents(GameObject root)
        {
            // Generated visuals should stay render-only and never add physics collider components.
            foreach (Component component in root.GetComponentsInChildren<Component>(true))
            {
                Assert.IsFalse(component.GetType().Name.Contains("Collider"), $"{component.name} should not have a collider component.");
            }
        }

        private static Transform FindNamedDescendant(Transform root, string expectedName)
        {
            // A missing source root cannot contain an imported rig bone.
            if (root == null)
            {
                return null;
            }

            // Include the supplied root so the helper remains correct for any future direct-bone call.
            foreach (Transform descendant in root.GetComponentsInChildren<Transform>(true))
            {
                // Exact names verify the Character Creator mapping without accepting similarly named accessory bones.
                if (descendant.name == expectedName)
                {
                    return descendant;
                }
            }

            // Returning null lets the caller's NUnit assertion report the missing expected bone clearly.
            return null;
        }

        private static Bounds CalculateEnabledRendererBounds(Transform root)
        {
            // Tests call this on named generated bodies, so a null transform means the hierarchy regressed.
            Assert.IsNotNull(root);

            // The first enabled renderer seeds the combined bounds without inventing a fake origin.
            bool hasBounds = false;

            // Unity bounds are world-space, which lets this compare differently parented survivor and zombie bodies.
            Bounds combinedBounds = default;

            // Include inactive descendants so renderer visibility, not object activation, decides the visual envelope.
            foreach (Renderer renderer in root.GetComponentsInChildren<Renderer>(true))
            {
                // Hidden legacy or fallback renderers should not contribute to the visible actor size.
                if (!renderer.enabled)
                {
                    continue;
                }

                if (!hasBounds)
                {
                    // Seed with the first real renderer to preserve its center and extents exactly.
                    combinedBounds = renderer.bounds;
                    hasBounds = true;
                    continue;
                }

                // Expanding bounds across all visible body parts gives a stable full-character height.
                combinedBounds.Encapsulate(renderer.bounds);
            }

            // A generated visible body without renderers would recreate the invisible-player failure mode.
            Assert.IsTrue(hasBounds, $"{root.name} should have at least one enabled renderer.");

            return combinedBounds;
        }

        private static Color ReadMaterialColor(Material material)
        {
            // URP Lit and Unlit expose _BaseColor as the primary tint.
            if (material.HasProperty("_BaseColor"))
            {
                return material.GetColor("_BaseColor");
            }

            // Built-in and simpler shaders usually expose _Color instead.
            if (material.HasProperty("_Color"))
            {
                return material.GetColor("_Color");
            }

            // The factory test should prevent this path, but keep a readable fallback for diagnostics.
            return material.color;
        }

        private static void AssertMaterialFloatIfPresent(Material material, string propertyName, float expectedValue)
        {
            // Shader-specific render-state controls should match the opaque contract whenever the shader exposes them.
            if (!material.HasProperty(propertyName))
            {
                return;
            }

            // Render-state properties are integer-like floats, but keep a tolerance for shader-family storage details.
            Assert.AreEqual(expectedValue, material.GetFloat(propertyName), 0.001f);
        }

        private static void InvokeAutoShooterUpdate(AutoShooter shooter)
        {
            // The shooter update is intentionally private in runtime code, so tests reflect it by exact name.
            MethodInfo updateMethod = typeof(AutoShooter).GetMethod("Update", BindingFlags.Instance | BindingFlags.NonPublic);

            // A missing method should fail with a clear test assertion instead of a NullReferenceException.
            Assert.IsNotNull(updateMethod);

            // Invoke once to simulate the first frame where the shot timer is ready.
            updateMethod.Invoke(shooter, null);
        }

        private static void InvokeSimpleCameraFollowLateUpdate(SimpleCameraFollow follow)
        {
            // The camera follow update is private runtime code, so tests reflect it by exact Unity callback name.
            MethodInfo lateUpdateMethod = typeof(SimpleCameraFollow).GetMethod("LateUpdate", BindingFlags.Instance | BindingFlags.NonPublic);

            // A missing method should fail with a clear test assertion instead of a NullReferenceException.
            Assert.IsNotNull(lateUpdateMethod);

            // Invoke once to simulate the first frame after an aspect ratio change.
            lateUpdateMethod.Invoke(follow, null);
        }

        private static float CalculateHorizontalFieldOfView(float verticalFieldOfView, float aspect)
        {
            // Unity stores vertical FOV, while lane visibility depends on the derived horizontal angle.
            float halfVerticalRadians = verticalFieldOfView * 0.5f * Mathf.Deg2Rad;

            // Perspective projection converts vertical coverage into horizontal coverage through the aspect ratio.
            return Mathf.Atan(Mathf.Tan(halfVerticalRadians) * aspect) * 2f * Mathf.Rad2Deg;
        }

        private static Zombie CreateZombie(Vector3 position, int breachPenalty)
        {
            GameObject zombieObject = new("Zombie Under Test");
            zombieObject.transform.position = position;

            Zombie zombie = zombieObject.AddComponent<Zombie>();
            zombie.Configure(5f, breachPenalty, null);
            return zombie;
        }
    }
}
