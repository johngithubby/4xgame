using LaneSurvivor.Data;
using LaneSurvivor.Heroes;
using LaneSurvivor.Progression;
using LaneSurvivor.Rendering;
using LaneSurvivor.Save;
using LaneSurvivor.UI;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace LaneSurvivor.Gameplay
{
    public sealed class LevelManager : MonoBehaviour
    {
        // World-space TextMesh feedback is disabled until it is replaced with a depth-safe HUD overlay.
        public const bool WorldSpaceFeedbackEnabled = false;

        // World-space shot tracers are disabled because they can read as morphing gates in the chase camera.
        public const bool WorldSpaceShotTracersEnabled = false;

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
                // A short tracer makes automatic shooting visible without adding art assets.
                SpawnShotTracer(origin, target);
            }

            // Damage text helps explain why tougher zombies take several shots.
            SpawnFeedback($"-{damage:0.#}", target + Vector3.up * 0.55f, new Color(1f, 0.92f, 0.35f));
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

            // Persist the reward immediately so returning to Base shows the updated coin balance.
            SaveGameManager.Save(saveData);
            return BuildRewardText(rewardCoins, heroReward, heroXpReward);
        }

        private static string BuildRewardText(int rewardCoins, HeroDefinition heroReward, HeroXpRewardResult heroXpReward)
        {
            // Coins are always the first reward line for a successful minigame win.
            List<string> rewardLines = new()
            {
                $"+{rewardCoins} coins"
            };

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
                GameObject zombieObject = PrototypeGeometryFactory.CreateCube("Zombie", zombiePosition, new Vector3(GameplayVisuals.ZombieCardWidth, GameplayVisuals.ZombieCardHeight, GameplayVisuals.ZombieCardDepth), zombieMaterial);

                Zombie zombie = zombieObject.AddComponent<Zombie>();
                zombie.Configure(zombieDefinition.health, zombieDefinition.breachPenalty, zombieMaterial);
                autoShooter.RegisterZombie(zombie);
                zombies.Add(zombie);
            }
        }

        private void ApplyReachedGates()
        {
            foreach (Gate gate in gates)
            {
                if (gate.TryResolve(playerSquad, levelDefinition.laneMatchTolerance))
                {
                    string message = gate.LastResolutionApplied ? gate.DisplayText : "MISS";
                    Color color = gate.LastResolutionApplied ? Color.white : new Color(0.75f, 0.75f, 0.75f);
                    SpawnFeedback(message, gate.transform.position + Vector3.up * 1.5f, color);
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
                    SpawnFeedback(message, zombie.transform.position + Vector3.up * 1.4f, color);
                }
            }
        }

        private void SpawnShotTracer(Vector3 origin, Vector3 target)
        {
            // Midpoint, length, and rotation turn a cube into a temporary laser-like line.
            Vector3 direction = target - origin;
            float distance = direction.magnitude;
            if (distance <= 0.01f)
            {
                return;
            }

            // Tracers are short-lived generated cubes stretched along the shot direction.
            Material tracerMaterial = PrototypeMaterialFactory.Create(new Color(1f, 0.82f, 0.16f));
            GameObject tracer = PrototypeGeometryFactory.CreateCube("Shot Tracer", origin + direction * 0.5f, new Vector3(0.08f, 0.08f, distance), tracerMaterial);
            tracer.transform.rotation = Quaternion.LookRotation(direction.normalized);
            Destroy(tracer, 0.08f);
        }

        private static TextMesh CreateWorldLabel(string text, Vector3 position, Color color, float scale)
        {
            // TextMesh keeps placeholder feedback independent from imported fonts or sprites.
            GameObject labelObject = new(text);
            labelObject.transform.position = position;
            labelObject.transform.rotation = Quaternion.Euler(65f, 0f, 0f);
            labelObject.transform.localScale = Vector3.one * scale;

            TextMesh label = labelObject.AddComponent<TextMesh>();
            label.text = text;
            label.anchor = TextAnchor.MiddleCenter;
            label.alignment = TextAlignment.Center;
            label.characterSize = 1f;
            label.color = color;
            return label;
        }

        private static void SpawnFeedback(string message, Vector3 position, Color color)
        {
            if (!WorldSpaceFeedbackEnabled)
            {
                // TextMesh feedback can depth-occlude the squad in the portrait chase camera, so Phase 1 uses HUD state and tracers instead.
                return;
            }

            // Feedback labels float upward and self-destroy, so no manager bookkeeping is needed.
            TextMesh label = CreateWorldLabel(message, position, color, 0.32f);
            label.gameObject.AddComponent<FloatingFeedback>().Configure(message, color, 1.1f);
        }
    }
}
