using LaneSurvivor.Gameplay;
using UnityEngine;
using UnityEngine.UI;

namespace LaneSurvivor.UI
{
    public sealed class MinigameHudController : MonoBehaviour
    {
        [SerializeField]
        private Text squadCountText;

        [SerializeField]
        private Text progressText;

        [SerializeField]
        private Text stateText;

        [SerializeField]
        private Button startButton;

        private LevelManager levelManager;

        private PlayerSquad playerSquad;

        private float finishDistance;

        public void Configure(Text squadLabel, Text progressLabel, Text stateLabel, Button startLevelButton)
        {
            squadCountText = squadLabel;
            progressText = progressLabel;
            stateText = stateLabel;
            startButton = startLevelButton;
        }

        public void Initialize(LevelManager manager, PlayerSquad squad, float levelFinishDistance)
        {
            levelManager = manager;
            playerSquad = squad;
            finishDistance = Mathf.Max(1f, levelFinishDistance);

            playerSquad.SquadCountChanged += UpdateSquadCount;
            startButton.onClick.AddListener(levelManager.BeginLevel);

            UpdateSquadCount(playerSquad.SquadCount);
            UpdateProgress();
        }

        public void SetState(LevelState state)
        {
            stateText.text = state switch
            {
                LevelState.Ready => "Ready",
                LevelState.Playing => "Running",
                LevelState.Won => "Complete",
                LevelState.Lost => "Defeated",
                _ => string.Empty
            };

            startButton.gameObject.SetActive(state == LevelState.Ready);
        }

        private void Update()
        {
            UpdateProgress();
        }

        private void UpdateSquadCount(int count)
        {
            squadCountText.text = $"Squad: {count}";
        }

        private void UpdateProgress()
        {
            if (playerSquad == null)
            {
                return;
            }

            float progress = Mathf.Clamp01(playerSquad.transform.position.z / finishDistance);
            progressText.text = $"Progress: {progress:P0}";
        }
    }
}
