using LaneSurvivor.Data;
using LaneSurvivor.Heroes;
using LaneSurvivor.Progression;
using LaneSurvivor.Save;
using LaneSurvivor.UI;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace LaneSurvivor.Gameplay
{
    public sealed class LevelManager : MonoBehaviour
    {
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

            // Persist the reward immediately so returning to Base shows the updated coin balance.
            SaveGameManager.Save(saveData);
            return heroReward != null
                ? $"+{rewardCoins} coins\nHero: {heroReward.displayName}"
                : $"+{rewardCoins} coins";
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
            GameObject track = GameObject.CreatePrimitive(PrimitiveType.Cube);
            track.name = "Runtime Track";
            track.transform.position = new Vector3(0f, -0.08f, levelDefinition.finishDistance * 0.5f);
            track.transform.localScale = new Vector3(7f, 0.1f, levelDefinition.finishDistance + 8f);

            Renderer trackRenderer = track.GetComponent<Renderer>();
            if (trackRenderer != null)
            {
                trackRenderer.sharedMaterial = trackMaterial;
            }

            GameObject finish = GameObject.CreatePrimitive(PrimitiveType.Cube);
            finish.name = "Finish Line";
            finish.transform.position = new Vector3(0f, 0.05f, levelDefinition.finishDistance);
            finish.transform.localScale = new Vector3(7f, 0.12f, 0.4f);
            SetPrimitiveColor(finish, new Color(0.25f, 0.95f, 0.42f));
            CreateWorldLabel("FINISH", new Vector3(0f, 0.5f, levelDefinition.finishDistance + 0.25f), Color.white, 0.45f);

            foreach (float laneX in levelDefinition.lanePositions)
            {
                GameObject laneMarker = GameObject.CreatePrimitive(PrimitiveType.Cube);
                laneMarker.name = "Lane Marker";
                laneMarker.transform.position = new Vector3(laneX, 0.02f, levelDefinition.finishDistance * 0.5f);
                laneMarker.transform.localScale = new Vector3(0.08f, 0.04f, levelDefinition.finishDistance + 8f);
                SetPrimitiveColor(laneMarker, new Color(0.82f, 0.86f, 0.88f));
            }
        }

        private void BuildGates()
        {
            foreach (GateSpawnDefinition gateDefinition in levelDefinition.gates)
            {
                GameObject gateObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
                gateObject.name = $"Gate {gateDefinition.modifierType}";
                gateObject.transform.localScale = new Vector3(2.8f, 2.2f, 0.35f);

                GameObject labelObject = new("Gate Label");
                labelObject.transform.SetParent(gateObject.transform, false);
                labelObject.transform.localPosition = new Vector3(0f, 0.8f, -0.22f);
                labelObject.transform.localRotation = Quaternion.Euler(70f, 0f, 0f);
                labelObject.transform.localScale = Vector3.one * 0.28f;

                TextMesh label = labelObject.AddComponent<TextMesh>();
                label.anchor = TextAnchor.MiddleCenter;
                label.alignment = TextAlignment.Center;
                label.characterSize = 1.5f;
                label.color = Color.white;

                Gate gate = gateObject.AddComponent<Gate>();
                gate.Configure(gateDefinition, gateMaterial, label);
                gates.Add(gate);
            }
        }

        private void BuildZombies()
        {
            foreach (ZombieSpawnDefinition zombieDefinition in levelDefinition.zombies)
            {
                GameObject zombieObject = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                zombieObject.name = "Zombie";
                zombieObject.transform.position = zombieDefinition.position;
                zombieObject.transform.localScale = new Vector3(0.8f, 1.1f, 0.8f);

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

        private static void SetPrimitiveColor(GameObject primitive, Color color)
        {
            // Runtime primitives each receive their own material instance when using renderer.material.
            Renderer renderer = primitive.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.material.color = color;
            }
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
            // Feedback labels float upward and self-destroy, so no manager bookkeeping is needed.
            TextMesh label = CreateWorldLabel(message, position, color, 0.32f);
            label.gameObject.AddComponent<FloatingFeedback>().Configure(message, color, 1.1f);
        }
    }
}
