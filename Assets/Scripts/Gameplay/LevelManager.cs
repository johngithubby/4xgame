using LaneSurvivor.Data;
using LaneSurvivor.Heroes;
using LaneSurvivor.Progression;
using LaneSurvivor.Rendering;
using LaneSurvivor.Retention;
using LaneSurvivor.Save;
using LaneSurvivor.UI;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace LaneSurvivor.Gameplay
{
    public sealed class LevelManager : MonoBehaviour
    {
        // World-space TextMesh feedback is enabled through depth-safe, camera-facing foreground materials.
        public const bool WorldSpaceFeedbackEnabled = true;

        // World-space shot tracers use thin camera-facing strips instead of gate-like stretched cubes.
        public const bool WorldSpaceShotTracersEnabled = true;

        [SerializeField]
        private LevelDefinition levelDefinition;

        [SerializeField]
        private PlayerSquad playerSquad;

        [SerializeField]
        private AutoShooter autoShooter;

        [SerializeField]
        private MinigameHudController hudController;

        [SerializeField]
        private EndScreenController endScreenController;

        [SerializeField]
        private Material trackMaterial;

        [SerializeField]
        private Material gateMaterial;

        [SerializeField]
        private Material zombieMaterial;

        [SerializeField]
        private bool autoStartOnPlay = true;

        [SerializeField]
        private float autoStartDelay = 0.5f;

        [SerializeField]
        private string baseSceneName = "Base";

        private LevelState state = LevelState.Ready;

        private readonly List<Gate> gates = new();

        private readonly List<Zombie> zombies = new();

        private float startTimer;

        private bool winRewardClaimed;

        public LevelState State => state;

        public void Configure(
            LevelDefinition definition,
            PlayerSquad squad,
            AutoShooter shooter,
            MinigameHudController hud,
            EndScreenController endScreen,
            Material track,
            Material gate,
            Material zombie)
        {
            levelDefinition = definition;
            playerSquad = squad;
            autoShooter = shooter;
            hudController = hud;
            endScreenController = endScreen;
            trackMaterial = track;
            gateMaterial = gate;
            zombieMaterial = zombie;
        }

        private void Start()
        {
            playerSquad.Initialize(levelDefinition);
            playerSquad.Defeated += HandlePlayerDefeated;

            autoShooter.Initialize(playerSquad, levelDefinition.shootRange, levelDefinition.shotInterval, levelDefinition.laneMatchTolerance);
            autoShooter.ShotFired += HandleShotFired;

            hudController.Initialize(this, playerSquad, levelDefinition.finishDistance);
            endScreenController.Initialize(RestartLevel, ReturnToBase);

            BuildRuntimeLevel();
            SetState(LevelState.Ready);
            startTimer = autoStartDelay;
            winRewardClaimed = false;
        }

        private void Update()
        {
            if (state == LevelState.Ready && autoStartOnPlay)
            {
                startTimer -= Time.deltaTime;
                if (startTimer <= 0f)
                {
                    BeginLevel();
                }
            }

            if (state != LevelState.Playing)
            {
                return;
            }

            ApplyReachedGates();
            ResolveZombieBreaches();

            LevelState evaluatedState = LevelStateEvaluator.Evaluate(
                playerSquad.transform.position.z,
                levelDefinition.finishDistance,
                playerSquad.SquadCount,
                state);

            if (evaluatedState != state)
            {
                SetState(evaluatedState);
            }
        }

        public void BeginLevel()
        {
            if (state != LevelState.Ready)
            {
                return;
            }

            SetState(LevelState.Playing);
        }

        private void RestartLevel()
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        private void ReturnToBase()
        {
            // Scene-name navigation keeps the local prototype independent from a backend or flow manager.
            SceneManager.LoadScene(baseSceneName);
        }

        private void HandlePlayerDefeated()
        {
            SetState(LevelState.Lost);
        }

        private void HandleShotFired(Vector3 origin, Vector3 target, float damage)
        {
            if (WorldSpaceShotTracersEnabled)
            {
                // A short line tracer makes automatic shooting visible without adding imported art assets.
                SpawnShotTracer(origin, target);
            }

            // Damage text helps explain why tougher zombies take several shots.
            SpawnFeedback($"-{damage:0.#}", LiftFeedbackPoint(target, GameplayVisuals.FeedbackLabelOffsetY), new Color(1f, 0.92f, 0.35f));
        }

        private void SetState(LevelState nextState)
        {
            state = nextState;
            playerSquad.SetMoving(state == LevelState.Playing);
            hudController.SetState(state);

            if (state == LevelState.Won)
            {
                string rewardText = ClaimWinReward();
                endScreenController.Show("Level Complete", rewardText);
            }
            else if (state == LevelState.Lost)
            {
                endScreenController.Show("Squad Lost");
            }
            else
            {
                endScreenController.Hide();
            }
        }

        private string ClaimWinReward()
        {
            // Load the current local save so rewards stack with any base progress made before the run.
            SaveGameData saveData = SaveGameManager.Load();

            // The progression rule owns the one-time-per-run guard through this manager's claim flag.
            int rewardCoins = PlayerProgression.TryClaimMinigameWinReward(saveData, ref winRewardClaimed);
            if (rewardCoins <= 0)
            {
                return string.Empty;
            }

            // The first gameplay win also grants the first local hero, then auto-equips it.
            HeroDefinition heroReward = HeroRewardSystem.TryGrantFirstWinHero(saveData);

            // Award hero XP after first-win hero grants so the new hero can progress immediately.
            HeroXpRewardResult heroXpReward = HeroProgression.TryGrantMinigameWinXp(saveData);

            // Mission completion unlocks the next local layout when the player clears the frontier mission.
            MissionCompletionResult missionCompletion = PlayerProgression.TryCompleteMission(saveData, levelDefinition.levelNumber);

            // Daily objective progress gives successful runs a local session goal beyond mission unlocks.
            DailyObjectiveStatus dailyObjectiveStatus = DailyObjectiveProgression.RecordMinigameWin(saveData, System.DateTime.UtcNow);

            // Persist the reward immediately so returning to Base shows the updated coin balance.
            SaveGameManager.Save(saveData);
            return BuildRewardText(rewardCoins, heroReward, heroXpReward, missionCompletion, dailyObjectiveStatus);
        }

        private static string BuildRewardText(int rewardCoins, HeroDefinition heroReward, HeroXpRewardResult heroXpReward, MissionCompletionResult missionCompletion, DailyObjectiveStatus dailyObjectiveStatus)
        {
            // Coins are always the first reward line for a successful minigame win.
            List<string> rewardLines = new()
            {
                $"+{rewardCoins} coins"
            };

            // Mission unlocks are shown before hero XP so the next run objective is immediately obvious.
            if (missionCompletion.unlockedNewMission)
            {
                rewardLines.Add($"Mission {missionCompletion.unlockedMissionLevel} unlocked: {PlayerProgression.GetMissionName(missionCompletion.unlockedMissionLevel)}");
            }

            // Daily objective feedback tells the player whether the Base claim button is ready.
            if (dailyObjectiveStatus.canClaimReward)
            {
                rewardLines.Add($"Daily objective ready: claim +{dailyObjectiveStatus.rewardCoins}c at Base");
            }
            else if (!dailyObjectiveStatus.rewardClaimed)
            {
                rewardLines.Add($"Daily objective: {dailyObjectiveStatus.wins}/{dailyObjectiveStatus.winsRequired} wins");
            }

            // First-win hero unlocks remain visible even when XP is also awarded.
            if (heroReward != null)
            {
                rewardLines.Add($"Hero: {heroReward.displayName}");
            }

            // Hero XP text names the receiving hero and level so progression is visible immediately.
            if (heroXpReward != null && heroXpReward.HasReward)
            {
                string levelUpText = heroXpReward.levelsGained > 0 ? " LEVEL UP" : string.Empty;
                rewardLines.Add($"{heroXpReward.hero.displayName}: +{heroXpReward.xpAdded} XP Lv {heroXpReward.level}{levelUpText}");
            }

            return string.Join("\n", rewardLines);
        }

        private void BuildRuntimeLevel()
        {
            gates.Clear();
            zombies.Clear();
            BuildTrack();
            BuildGates();
            BuildZombies();
        }

        private void BuildTrack()
        {
            // The track is a collider-free flat plane so it cannot occlude squad, gate, or zombie placeholders.
            PrototypeGeometryFactory.CreateHorizontalPlane("Runtime Track", new Vector3(0f, GameplayVisuals.TrackSurfaceY, levelDefinition.finishDistance * 0.5f), new Vector2(GameplayVisuals.TrackWidth, levelDefinition.finishDistance + 8f), trackMaterial);

            // The finish strip is a flat decal so it cannot show a raised side face at the road edge.
            Material finishMaterial = PrototypeMaterialFactory.Create(new Color(0.25f, 0.95f, 0.42f));
            PrototypeGeometryFactory.CreateHorizontalPlane("Finish Line", new Vector3(0f, GameplayVisuals.FinishLineY, levelDefinition.finishDistance), new Vector2(GameplayVisuals.TrackWidth, GameplayVisuals.FinishLineDepth), finishMaterial);
            CreateWorldLabel("FINISH", new Vector3(0f, GameplayVisuals.WorldLabelY, levelDefinition.finishDistance + 0.25f), Color.white, 0.45f);
            CreateWorldLabel($"LEVEL {levelDefinition.levelNumber}", new Vector3(0f, GameplayVisuals.WorldLabelY, 2f), Color.white, 0.38f);

            // Lane markers share one pale material because they are repeated static guide strips.
            Material laneMaterial = PrototypeMaterialFactory.Create(new Color(0.82f, 0.86f, 0.88f));
            foreach (float laneX in levelDefinition.lanePositions)
            {
                // Each marker spans the course to show the three-lane play space.
                PrototypeGeometryFactory.CreateCube("Lane Marker", new Vector3(laneX, GameplayVisuals.TrackTopY + 0.03f, levelDefinition.finishDistance * 0.5f), new Vector3(0.08f, 0.04f, levelDefinition.finishDistance + 8f), laneMaterial);
            }
        }

        private void BuildGates()
        {
            foreach (GateSpawnDefinition gateDefinition in levelDefinition.gates)
            {
                // Gate roots stay at lane/distance positions while high children provide stable visible markers.
                Vector3 gatePosition = GameplayVisuals.WithVisualY(gateDefinition.position, GameplayVisuals.GateRootY);
                GateSpawnDefinition visualGateDefinition = gateDefinition;
                visualGateDefinition.position = gatePosition;
                GameObject gateObject = CreateGateMarker($"Gate {gateDefinition.modifierType}", gatePosition, gateMaterial);

                GameObject labelObject = new("Gate Label");
                labelObject.transform.SetParent(gateObject.transform, false);
                // The label rides on the high card face so it cannot appear before or after the marker body.
                labelObject.transform.localPosition = new Vector3(0f, GameplayVisuals.GateCardCenterY - GameplayVisuals.GateRootY, GameplayVisuals.GateFaceOffsetZ - GameplayVisuals.GateCardDepth * 0.55f);
                labelObject.transform.localRotation = Quaternion.Euler(0f, 0f, 0f);
                labelObject.transform.localScale = Vector3.one * 0.22f;

                TextMesh label = labelObject.AddComponent<TextMesh>();
                label.anchor = TextAnchor.MiddleCenter;
                label.alignment = TextAlignment.Center;
                label.characterSize = 1.5f;
                label.color = Color.white;

                Gate gate = gateObject.AddComponent<Gate>();
                gate.Configure(visualGateDefinition, gateMaterial, label);
                gates.Add(gate);
            }
        }

        private static GameObject CreateGateMarker(string name, Vector3 position, Material material)
        {
            // An empty root keeps gate logic at the lane center while child meshes form a readable marker.
            GameObject root = new(name);
            root.transform.position = position;

            // Visuals stay in the gameplay lane; low road pieces are limited to flat decals.
            Vector3 visualPosition = position;

            if (GameplayVisuals.GateFootprintEnabled)
            {
                // The footprint is decorative only; the high card is the always-visible gameplay marker.
                GameObject footprint = PrototypeGeometryFactory.CreateHorizontalPlane("Gate Footprint", visualPosition + new Vector3(0f, GameplayVisuals.GateFootprintY - GameplayVisuals.GateRootY, 0f), new Vector2(GameplayVisuals.GateFootprintWidth, GameplayVisuals.GateFootprintDepth), material);
                footprint.transform.SetParent(root.transform, true);
            }

            // The card sits fully above the road horizon so it scales consistently instead of revealing legs later.
            GameObject card = PrototypeGeometryFactory.CreateCube("Gate Card", visualPosition + new Vector3(0f, GameplayVisuals.GateCardCenterY - GameplayVisuals.GateRootY, GameplayVisuals.GateFaceOffsetZ), new Vector3(GameplayVisuals.GateCardWidth, GameplayVisuals.GateCardHeight, GameplayVisuals.GateCardDepth), material);
            card.transform.SetParent(root.transform, true);

            return root;
        }

        private void BuildZombies()
        {
            foreach (ZombieSpawnDefinition zombieDefinition in levelDefinition.zombies)
            {
                // Zombies use high cards so they do not pop from below the road horizon near the player.
                Vector3 zombiePosition = GameplayVisuals.WithVisualY(zombieDefinition.position, GameplayVisuals.ZombieCenterY);
                Vector3 zombieSize = GetZombieCardSize(zombieDefinition.enemyType);
                GameObject zombieObject = PrototypeGeometryFactory.CreateCube(GetZombieObjectName(zombieDefinition.enemyType), zombiePosition, zombieSize, zombieMaterial);
                CreateZombieTypeLabel(zombieObject.transform, zombieDefinition.enemyType);

                Zombie zombie = zombieObject.AddComponent<Zombie>();
                zombie.Configure(zombieDefinition.health, zombieDefinition.breachPenalty, zombieMaterial, zombieDefinition.enemyType);
                autoShooter.RegisterZombie(zombie);
                zombies.Add(zombie);
            }
        }

        private static Vector3 GetZombieCardSize(ZombieEnemyType enemyType)
        {
            // Armored zombies are slightly wider and taller so their reduced-damage behavior has a visible tell.
            if (enemyType == ZombieEnemyType.Armored)
            {
                return new Vector3(GameplayVisuals.ZombieCardWidth * 1.15f, GameplayVisuals.ZombieCardHeight * 1.12f, GameplayVisuals.ZombieCardDepth);
            }

            // Basic zombies keep the established high-card placeholder silhouette.
            return new Vector3(GameplayVisuals.ZombieCardWidth, GameplayVisuals.ZombieCardHeight, GameplayVisuals.ZombieCardDepth);
        }

        private static string GetZombieObjectName(ZombieEnemyType enemyType)
        {
            // Object names make PlayMode smoke failures easier to read in the generated hierarchy.
            return enemyType == ZombieEnemyType.Armored ? "Armored Zombie" : "Zombie";
        }

        private static void CreateZombieTypeLabel(Transform zombieTransform, ZombieEnemyType enemyType)
        {
            // Basic zombies need no extra label because they are the default enemy read.
            if (enemyType != ZombieEnemyType.Armored)
            {
                return;
            }

            // A tiny label makes the new enemy type understandable without imported art.
            GameObject labelObject = new("Zombie Type Label");
            labelObject.transform.SetParent(zombieTransform, false);
            labelObject.transform.localPosition = new Vector3(0f, GameplayVisuals.ZombieCardHeight * 0.42f, -GameplayVisuals.ZombieCardDepth * 0.6f);
            labelObject.transform.localRotation = Quaternion.identity;
            labelObject.transform.localScale = Vector3.one * 0.16f;

            // TextMesh matches the existing prototype world labels and keeps dependencies small.
            TextMesh label = labelObject.AddComponent<TextMesh>();
            label.text = "ARMOR";
            label.font = ResolveWorldTextFont(label.font);
            label.anchor = TextAnchor.MiddleCenter;
            label.alignment = TextAlignment.Center;
            label.characterSize = 1f;
            label.color = Color.white;
        }

        private void ApplyReachedGates()
        {
            foreach (Gate gate in gates)
            {
                if (gate.TryResolve(playerSquad, levelDefinition.laneMatchTolerance))
                {
                    string message = gate.LastResolutionApplied ? gate.DisplayText : "MISS";
                    Color color = gate.LastResolutionApplied ? Color.white : new Color(0.75f, 0.75f, 0.75f);
                    SpawnFeedback(message, LiftFeedbackPoint(gate.transform.position, GameplayVisuals.FeedbackLabelOffsetY), color);
                }
            }
        }

        private void ResolveZombieBreaches()
        {
            foreach (Zombie zombie in zombies)
            {
                if (zombie.TryBreach(playerSquad, levelDefinition.laneMatchTolerance))
                {
                    string message = zombie.LastBreachApplied ? $"-{zombie.BreachPenalty}" : "DODGED";
                    Color color = zombie.LastBreachApplied ? new Color(1f, 0.25f, 0.20f) : new Color(0.45f, 0.9f, 1f);
                    SpawnFeedback(message, LiftFeedbackPoint(zombie.transform.position, GameplayVisuals.FeedbackLabelOffsetY), color);
                }
            }
        }

        private void SpawnShotTracer(Vector3 origin, Vector3 target)
        {
            // Move the visible muzzle forward so the tracer does not project behind the HUD-followed player marker.
            Vector3 muzzleOrigin = MoveTracerOriginToMuzzle(origin, target);

            // Lift endpoints into the readable actor-card band so the road surface cannot swallow the effect.
            Vector3 liftedOrigin = OffsetTracerPoint(LiftTracerPoint(muzzleOrigin));
            Vector3 liftedTarget = OffsetTracerPoint(LiftTracerPoint(target));

            // Direction and distance guard against invalid zero-length tracer geometry.
            Vector3 direction = liftedTarget - liftedOrigin;
            float distance = direction.magnitude;
            if (distance <= 0.01f)
            {
                return;
            }

            // The tracer object is real world-space feedback, but its mesh is a flat billboard instead of a cube.
            GameObject tracer = new("Shot Tracer");

            // The generated material ignores depth so cards and the road cannot bury the tracer on iOS.
            Material tracerMaterial = PrototypeMaterialFactory.CreateAlwaysVisibleSolidFeedback(new Color(1f, 0.42f, 0.02f, 1f));

            // A four-vertex quad is more reliable in iOS player builds than a procedural LineRenderer.
            Mesh tracerMesh = CreateShotTracerMesh(liftedOrigin, liftedTarget, direction.normalized);

            // MeshFilter carries the generated flat strip geometry.
            MeshFilter meshFilter = tracer.AddComponent<MeshFilter>();
            meshFilter.sharedMesh = tracerMesh;

            // MeshRenderer draws the strip through the solid foreground material.
            MeshRenderer meshRenderer = tracer.AddComponent<MeshRenderer>();
            meshRenderer.sharedMaterial = tracerMaterial;
            meshRenderer.sortingOrder = 100;

            // The object, mesh, and material are all short-lived prototype effects.
            Destroy(tracer, GameplayVisuals.ShotTracerLifetimeSeconds);
            Destroy(tracerMesh, GameplayVisuals.ShotTracerLifetimeSeconds + 0.02f);
            Destroy(tracerMaterial, GameplayVisuals.ShotTracerLifetimeSeconds + 0.02f);
        }

        private static Mesh CreateShotTracerMesh(Vector3 origin, Vector3 target, Vector3 direction)
        {
            // The camera right-facing vector makes the strip readable without giving it boxy side faces.
            Vector3 side = CalculateTracerSideVector(direction);

            // A slight taper gives the streak a projectile feel instead of a rigid obstacle silhouette.
            Vector3 startSide = side * (GameplayVisuals.ShotTracerWidth * 0.5f);
            Vector3 endSide = side * (GameplayVisuals.ShotTracerWidth * 0.28f);

            // Vertices are stored in world coordinates because the temporary object stays at the origin.
            Vector3[] vertices =
            {
                origin - startSide,
                origin + startSide,
                target + endSide,
                target - endSide
            };

            // Simple UVs keep the quad compatible with sprite and transparent unlit shaders.
            Vector2[] uvs =
            {
                Vector2.zero,
                Vector2.up,
                Vector2.one,
                Vector2.right
            };

            // Two front triangles plus two back triangles keep the tracer visible if the camera sees either side.
            int[] triangles =
            {
                0, 1, 2,
                0, 2, 3,
                0, 2, 1,
                0, 3, 2
            };

            // HideFlags prevent the temporary tracer mesh from being saved into the scene.
            Mesh mesh = new()
            {
                name = "Shot Tracer Mesh",
                hideFlags = HideFlags.HideAndDontSave,
                vertices = vertices,
                uv = uvs,
                triangles = triangles
            };

            // Bounds let Unity cull the short-lived strip correctly while it exists.
            mesh.RecalculateBounds();
            return mesh;
        }

        private static Vector3 CalculateTracerSideVector(Vector3 direction)
        {
            // Use the active camera so the strip faces the same view used by the iOS recording.
            Camera camera = Camera.main;
            Vector3 cameraForward = camera != null ? camera.transform.forward : Vector3.forward;

            // Cross product gives a vector perpendicular to both the shot direction and camera view.
            Vector3 side = Vector3.Cross(direction, cameraForward);
            if (side.sqrMagnitude > 0.0001f)
            {
                return side.normalized;
            }

            // If the shot happens to align with the camera, fall back to camera/world right for a visible width.
            return camera != null ? camera.transform.right : Vector3.right;
        }

        private static Vector3 MoveTracerOriginToMuzzle(Vector3 origin, Vector3 target)
        {
            // The visible player marker follows a clamped screen projection, so center-origin shots can appear behind it.
            Vector3 planarDirection = new(target.x - origin.x, 0f, target.z - origin.z);

            // Very close or overlapping targets should keep the original point instead of creating unstable geometry.
            float planarDistance = planarDirection.magnitude;
            if (planarDistance <= 0.01f)
            {
                return origin;
            }

            // Cap the offset to half the available distance so a close-range shot still points at the target.
            float forwardOffset = Mathf.Min(GameplayVisuals.ShotTracerMuzzleForwardOffsetZ, planarDistance * 0.5f);

            // Move only in the ground plane so the readability lift remains centralized in LiftTracerPoint.
            return origin + planarDirection.normalized * forwardOffset;
        }

        private static TextMesh CreateWorldLabel(string text, Vector3 position, Color color, float scale)
        {
            return CreateWorldLabel(text, position, color, scale, false);
        }

        private static TextMesh CreateWorldLabel(string text, Vector3 position, Color color, float scale, bool isFloatingFeedback)
        {
            // TextMesh keeps placeholder feedback independent from imported fonts or sprites.
            GameObject labelObject = new(text);
            labelObject.transform.position = position;
            labelObject.transform.rotation = isFloatingFeedback ? Quaternion.identity : Quaternion.Euler(65f, 0f, 0f);
            labelObject.transform.localScale = Vector3.one * scale;

            TextMesh label = labelObject.AddComponent<TextMesh>();
            label.text = text;
            label.font = ResolveWorldTextFont(label.font);
            label.anchor = TextAnchor.MiddleCenter;
            label.alignment = TextAlignment.Center;
            label.characterSize = 1f;
            label.color = color;

            if (isFloatingFeedback)
            {
                // Generated text materials render after world geometry and keep the font atlas intact.
                Renderer labelRenderer = label.GetComponent<Renderer>();
                labelRenderer.sharedMaterial = PrototypeMaterialFactory.CreateAlwaysVisibleText(label.font);

                // A small world-space shadow gives the label contrast without moving it into a HUD overlay.
                CreateFeedbackTextShadow(labelObject.transform, text, label.font);
            }

            return label;
        }

        private static void SpawnFeedback(string message, Vector3 position, Color color)
        {
            if (!WorldSpaceFeedbackEnabled)
            {
                // The compile-time switch stays available for emergency visual triage.
                return;
            }

            // Feedback labels float upward and self-destroy, so no manager bookkeeping is needed.
            TextMesh label = CreateWorldLabel(message, position, color, GameplayVisuals.FeedbackLabelScale, true);
            label.gameObject.AddComponent<FloatingFeedback>().Configure(message, color, 1.1f, Camera.main);
        }

        private static void CreateFeedbackTextShadow(Transform parent, string text, Font font)
        {
            // The shadow is a second TextMesh child, so it remains world-space and billboards with the parent label.
            GameObject shadowObject = new("Feedback Text Shadow");
            shadowObject.transform.SetParent(parent, false);
            shadowObject.transform.localPosition = GameplayVisuals.FeedbackTextShadowOffset;
            shadowObject.transform.localRotation = Quaternion.identity;
            shadowObject.transform.localScale = Vector3.one;

            // Match the primary glyph settings so the shadow tracks every character exactly.
            TextMesh shadow = shadowObject.AddComponent<TextMesh>();
            shadow.text = text;
            shadow.font = ResolveWorldTextFont(font);
            shadow.anchor = TextAnchor.MiddleCenter;
            shadow.alignment = TextAlignment.Center;
            shadow.characterSize = 1f;
            shadow.color = new Color(0f, 0f, 0f, 0.75f);

            // Give the shadow the same foreground depth behavior as the primary label.
            Renderer shadowRenderer = shadow.GetComponent<Renderer>();
            shadowRenderer.sharedMaterial = PrototypeMaterialFactory.CreateAlwaysVisibleText(shadow.font);
        }

        private static Vector3 LiftFeedbackPoint(Vector3 point, float offsetY)
        {
            // Preserve the event lane and distance while moving the label into the readable card band.
            return new Vector3(point.x, Mathf.Max(point.y + offsetY, GameplayVisuals.ShotTracerMinimumY), point.z);
        }

        private static Vector3 LiftTracerPoint(Vector3 point)
        {
            // Keep shot endpoints above the road and aligned to their zombie/squad world X/Z positions.
            return new Vector3(point.x, Mathf.Max(point.y, GameplayVisuals.ShotTracerMinimumY), point.z);
        }

        private static Vector3 OffsetTracerPoint(Vector3 point)
        {
            // Center-lane shots overlap the lane stripe unless the tracer rides slightly to one side.
            float offsetDirection = point.x < 0f ? -1f : 1f;

            // Side-lane shots offset outward, while center-lane shots consistently offset to the right edge.
            return new Vector3(point.x + offsetDirection * GameplayVisuals.ShotTracerLaneOffsetX, point.y, point.z);
        }

        private static Font ResolveWorldTextFont(Font requestedFont)
        {
            // Keep Unity's default TextMesh font when available because it already owns a glyph atlas.
            if (requestedFont != null)
            {
                return requestedFont;
            }

            // Unity 6 exposes LegacyRuntime.ttf as the runtime-safe built-in font.
            Font legacyFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (legacyFont != null)
            {
                return legacyFont;
            }

            // Older editor/test contexts may still expose Arial.ttf instead.
            return Resources.GetBuiltinResource<Font>("Arial.ttf");
        }
    }
}
