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
            bool originUsedWeaponMuzzle = false;
            Vector3 actualOrigin = default;

            try
            {
                // The authored lane setup matches runtime enough to initialize the squad and shooter deterministically.
                levelDefinition.startingSquadCount = 5;
                levelDefinition.startingDamagePerMember = 1f;
                levelDefinition.squadMoveSpeed = 1f;
                levelDefinition.lanePositions = new[] { -GameplayVisuals.SideLaneX, 0f, GameplayVisuals.SideLaneX };

                // Initialization scans the generated hierarchy and registers all survivor muzzle anchors.
                squad.Initialize(levelDefinition);
                Assert.AreEqual(3, squad.WeaponMuzzleCount);

                // The first round-robin shot should come from the leader rifle muzzle.
                Transform expectedMuzzle = playerObject.transform.Find($"Survivor Leader/Human Arm Right/Human Hand Right/{PrototypeCharacterFactory.LeaderRifleName}/{PlayerSquad.WeaponMuzzleAnchorName}");
                Assert.IsNotNull(expectedMuzzle);

                // Configure the same target path used by normal gameplay.
                shooter.Initialize(squad, 8f, 0.35f, 0.5f);
                shooter.RegisterZombie(zombie);
                shooter.ShotFired += (origin, _, _, usesWeaponMuzzle) =>
                {
                    shotFired = true;
                    actualOrigin = origin;
                    originUsedWeaponMuzzle = usesWeaponMuzzle;
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
                Assert.IsNotNull(player.transform.Find("Survivor Leader/Human Leg Left/Human Knee Left"));
                Assert.IsNotNull(player.transform.Find("Survivor Leader/Human Leg Left/Human Knee Left/Human Shin Left"));
                Assert.IsNotNull(player.transform.Find($"Survivor Leader/Human Arm Right/Human Hand Right/{PrototypeCharacterFactory.LeaderRifleName}"));
                Assert.IsNotNull(player.transform.Find($"Survivor Left Wing/Human Arm Right/Human Hand Right/{PrototypeCharacterFactory.LeftWingShotgunName}"));
                Assert.IsNotNull(player.transform.Find($"Survivor Right Wing/Human Arm Right/Human Hand Right/{PrototypeCharacterFactory.RightWingSmgName}"));
                Assert.IsNotNull(player.transform.Find("Survivor Right Wing/Human Leg Right/Human Knee Right/Human Shin Right/Human Boot Right"));
                Assert.GreaterOrEqual(player.GetComponentsInChildren<MeshRenderer>().Length, 30);

                // Every distinct weapon profile should own a direct muzzle anchor at the visible barrel tip.
                AssertWeaponMuzzle(player.transform, "Survivor Leader", PrototypeCharacterFactory.LeaderRifleName);
                AssertWeaponMuzzle(player.transform, "Survivor Left Wing", PrototypeCharacterFactory.LeftWingShotgunName);
                AssertWeaponMuzzle(player.transform, "Survivor Right Wing", PrototypeCharacterFactory.RightWingSmgName);

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

                // Capture rest-pose leg joint rotations before forcing a run step.
                Transform playerLeg = player.transform.Find("Survivor Leader/Human Leg Left");
                Transform playerKnee = player.transform.Find("Survivor Leader/Human Leg Left/Human Knee Left");
                Transform playerShin = player.transform.Find("Survivor Leader/Human Leg Left/Human Knee Left/Human Shin Left");
                Assert.IsNotNull(playerLeg);
                Assert.IsNotNull(playerKnee);
                Assert.IsNotNull(playerShin);
                Assert.AreSame(playerLeg, playerKnee.parent);
                Assert.AreSame(playerKnee, playerShin.parent);
                Quaternion playerLegRestRotation = playerLeg.localRotation;
                Quaternion playerKneeRestRotation = playerKnee.localRotation;

                // A forced moving evaluation should swing the hip and bend the connected knee joint.
                playerAnimator.ForceEvaluate(0.4f, true);
                Assert.IsTrue(playerAnimator.IsAnimating);
                float playerThighSwing = Quaternion.Angle(playerLegRestRotation, playerLeg.localRotation);
                float playerKneeBend = Quaternion.Angle(playerKneeRestRotation, playerKnee.localRotation);
                Assert.Greater(playerThighSwing, 0.1f);
                Assert.Greater(playerKneeBend, playerThighSwing + 5f);
                Assert.Less(Vector3.Distance(playerKnee.position, playerShin.position), 0.001f);

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
            Assert.Less(GameplayVisuals.ShotTracerWidth, GameplayVisuals.PlayerFootprint * 0.2f);
            Assert.Less(GameplayVisuals.ShotTracerWidth, GameplayVisuals.GateCardWidth * 0.1f);

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
            // The profile root must live under the right-hand chain so animation carries the weapon and muzzle together.
            Transform weapon = playerRoot.Find($"{survivorName}/Human Arm Right/Human Hand Right/{weaponName}");
            Assert.IsNotNull(weapon, $"{weaponName} should exist under {survivorName}'s right hand.");

            // Muzzle anchors are direct weapon children, not unrelated sibling transforms near the squad root.
            Transform muzzle = weapon.Find(PlayerSquad.WeaponMuzzleAnchorName);
            Assert.IsNotNull(muzzle, $"{weaponName} should have a named muzzle anchor.");
            Assert.AreSame(weapon, muzzle.parent);

            // The anchor should be authored above the tracer minimum so LevelManager can keep the start point exact.
            Assert.GreaterOrEqual(muzzle.position.y, GameplayVisuals.ShotTracerMinimumY);

            // The barrel tip needs to sit down-lane from the survivor body, because zombies spawn at larger Z values.
            Transform survivor = playerRoot.Find(survivorName);
            Assert.IsNotNull(survivor);
            Assert.Greater(muzzle.position.z, survivor.position.z + 0.2f);
        }

        private static void AssertNoColliderComponents(GameObject root)
        {
            // Generated visuals should stay render-only and never add physics collider components.
            foreach (Component component in root.GetComponentsInChildren<Component>(true))
            {
                Assert.IsFalse(component.GetType().Name.Contains("Collider"), $"{component.name} should not have a collider component.");
            }
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
