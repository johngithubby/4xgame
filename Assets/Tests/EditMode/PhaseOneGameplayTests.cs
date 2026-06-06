using LaneSurvivor.Data;
using LaneSurvivor.Gameplay;
using LaneSurvivor.Rendering;
using LaneSurvivor.Save;
using LaneSurvivor.UI;
using NUnit.Framework;
using System.Reflection;
using UnityEngine;

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
        public void LevelManager_DisablesWorldSpaceFeedbackUntilOverlayIsDepthSafe()
        {
            // World-space TextMesh labels previously occluded the player during the chase-camera run.
            Assert.IsFalse(LevelManager.WorldSpaceFeedbackEnabled);

            // World-space tracers previously read as shape-shifting gate pieces in full-run video proof.
            Assert.IsFalse(LevelManager.WorldSpaceShotTracersEnabled);
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
                shooter.ShotFired += (_, _, _) => shotFired = true;
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
                shooter.ShotFired += (_, _, _) => shotFired = true;
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
                foreach (Component component in cube.GetComponents<Component>())
                {
                    Assert.IsFalse(component.GetType().Name.Contains("Collider"));
                }
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
                foreach (Component component in plane.GetComponents<Component>())
                {
                    Assert.IsFalse(component.GetType().Name.Contains("Collider"));
                }
            }
            finally
            {
                // Destroy generated Unity objects explicitly so EditMode tests stay isolated.
                UnityEngine.Object.DestroyImmediate(plane);
                UnityEngine.Object.DestroyImmediate(material);
            }
        }

        [Test]
        public void GameplayVisuals_KeepActorsAboveTrackSurface()
        {
            // The squad bottom must stay visibly above the generated track surface.
            Assert.Greater(GameplayVisuals.PlayerCenterY - GameplayVisuals.PlayerHeight * 0.5f, GameplayVisuals.TrackTopY);

            // The squad needs substantial clearance so road perspective cannot swallow it mid-run.
            Assert.GreaterOrEqual(GameplayVisuals.PlayerCenterY - GameplayVisuals.PlayerHeight * 0.5f - GameplayVisuals.TrackTopY, 0.75f);

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

            // Zombie cards also sit above the horizon so they do not suddenly pop near the player.
            Assert.GreaterOrEqual(GameplayVisuals.ZombieCardBottomY - GameplayVisuals.TrackTopY, 0.95f);

            // Zombie bottoms also need clearance because they can sit near the same late-lane horizon.
            float zombieClearance = GameplayVisuals.ZombieCenterY - GameplayVisuals.ZombieHeight * 0.5f - GameplayVisuals.TrackTopY;
            Assert.GreaterOrEqual(zombieClearance, 0.95f);

            // World labels should sit above actor bases so finish and gate context remains readable.
            Assert.Greater(GameplayVisuals.WorldLabelY, GameplayVisuals.TrackTopY + 1f);
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
        public void GameplayVisuals_KeepVisibilityMastAboveGateTop()
        {
            // Simulator verification now uses an overlay player marker to avoid world-depth dropouts.
            Assert.IsTrue(GameplayVisuals.UseScreenSpacePlayerMarker);

            // The old world player mesh should not be visible at the same time as the overlay marker.
            Assert.IsFalse(GameplayVisuals.WorldPlayerMeshRenderersEnabled);

            // The overlay marker should remain compact enough to avoid the oversized-player regression.
            Assert.LessOrEqual(GameplayVisuals.ScreenPlayerMarkerWidth, 40f);
            Assert.LessOrEqual(GameplayVisuals.ScreenPlayerMarkerHeight, 52f);

            // The squad body should stay compact enough that it does not dominate the portrait camera.
            Assert.LessOrEqual(GameplayVisuals.PlayerHeight, 0.65f);

            // The visibility mast should be a small marker, not a giant replacement player.
            Assert.LessOrEqual(GameplayVisuals.PlayerMastHeight, 0.55f);

            // The high beacon should stay compact so it marks the squad without becoming the body.
            Assert.Less(GameplayVisuals.PlayerBeaconFootprint, GameplayVisuals.PlayerFootprint);

            // The mast top should remain below the card top so the player does not visually swallow the gate.
            float mastTop = GameplayVisuals.PlayerCenterY + GameplayVisuals.PlayerMastOffsetY + GameplayVisuals.PlayerMastHeight * 0.5f;
            float gateCardTop = GameplayVisuals.GateCardCenterY + GameplayVisuals.GateCardHeight * 0.5f;
            Assert.Less(mastTop, gateCardTop);
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

        private static void InvokeAutoShooterUpdate(AutoShooter shooter)
        {
            // The shooter update is intentionally private in runtime code, so tests reflect it by exact name.
            MethodInfo updateMethod = typeof(AutoShooter).GetMethod("Update", BindingFlags.Instance | BindingFlags.NonPublic);

            // A missing method should fail with a clear test assertion instead of a NullReferenceException.
            Assert.IsNotNull(updateMethod);

            // Invoke once to simulate the first frame where the shot timer is ready.
            updateMethod.Invoke(shooter, null);
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
