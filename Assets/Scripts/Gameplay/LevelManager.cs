using LaneSurvivor.Data;
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

        private LevelState state = LevelState.Ready;

        private readonly List<Gate> gates = new();

        private readonly List<Zombie> zombies = new();

        private float startTimer;

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
            endScreenController.Initialize(RestartLevel);

            BuildRuntimeLevel();
            SetState(LevelState.Ready);
            startTimer = autoStartDelay;
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
                endScreenController.Show("Level Complete");
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

            foreach (float laneX in levelDefinition.lanePositions)
            {
                GameObject laneMarker = GameObject.CreatePrimitive(PrimitiveType.Cube);
                laneMarker.name = "Lane Marker";
                laneMarker.transform.position = new Vector3(laneX, 0.02f, levelDefinition.finishDistance * 0.5f);
                laneMarker.transform.localScale = new Vector3(0.08f, 0.04f, levelDefinition.finishDistance + 8f);
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
                gate.TryResolve(playerSquad, levelDefinition.laneMatchTolerance);
            }
        }

        private void ResolveZombieBreaches()
        {
            foreach (Zombie zombie in zombies)
            {
                zombie.TryBreach(playerSquad, levelDefinition.laneMatchTolerance);
            }
        }
    }
}
