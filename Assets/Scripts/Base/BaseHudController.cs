using System;
using LaneSurvivor.Progression;
using LaneSurvivor.Save;
using UnityEngine;
using UnityEngine.UI;

namespace LaneSurvivor.Base
{
    public sealed class BaseHudController : MonoBehaviour
    {
        [SerializeField]
        private Text titleText;

        [SerializeField]
        private Text coinsText;

        [SerializeField]
        private Text hqText;

        [SerializeField]
        private Text timerText;

        [SerializeField]
        private Text statusText;

        [SerializeField]
        private Text playHintText;

        [SerializeField]
        private Button collectButton;

        [SerializeField]
        private Button upgradeButton;

        [SerializeField]
        private Button playButton;

        [SerializeField]
        private Button resetButton;

        public void Configure(Text title, Text coins, Text hq, Text timer, Text status, Text playHint, Button collect, Button upgrade, Button play, Button reset)
        {
            titleText = title;
            coinsText = coins;
            hqText = hq;
            timerText = timer;
            statusText = status;
            playHintText = playHint;
            collectButton = collect;
            upgradeButton = upgrade;
            playButton = play;
            resetButton = reset;
        }

        public void Initialize(Action collectAction, Action upgradeAction, Action playAction, Action resetAction)
        {
            // Replace listeners so scene rebuilds or test setup cannot accidentally duplicate clicks.
            collectButton.onClick.RemoveAllListeners();
            upgradeButton.onClick.RemoveAllListeners();
            playButton.onClick.RemoveAllListeners();
            resetButton.onClick.RemoveAllListeners();

            // Button listeners stay tiny and delegate all state changes to the bootstrap.
            collectButton.onClick.AddListener(() => collectAction?.Invoke());
            upgradeButton.onClick.AddListener(() => upgradeAction?.Invoke());
            playButton.onClick.AddListener(() => playAction?.Invoke());
            resetButton.onClick.AddListener(() => resetAction?.Invoke());

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            // Keep the reset affordance available in editor and development builds for fast iteration.
            resetButton.gameObject.SetActive(true);
#else
            // Hide reset from release builds so it remains a debug-only helper.
            resetButton.gameObject.SetActive(false);
#endif
        }

        public void UpdateView(SaveGameData saveData, int remainingSeconds, string statusOverride)
        {
            // Normalize the value read by UI so null data still shows a sensible first-launch state.
            int hqLevel = Mathf.Max(1, saveData?.hqLevel ?? 1);
            int coins = Mathf.Max(0, saveData?.coins ?? 0);
            int upgradeCost = PlayerProgression.GetHqUpgradeCost(hqLevel);
            bool upgradeRunning = saveData != null && saveData.hqUpgradeInProgress;
            bool canAffordUpgrade = coins >= upgradeCost;

            // Build the default status separately so action feedback can override it cleanly.
            string fallbackStatus = upgradeRunning
                ? "HQ upgrade in progress"
                : $"Next HQ upgrade: {upgradeCost} coins";

            titleText.text = "Base";
            coinsText.text = $"Coins: {coins}";
            hqText.text = $"HQ Level: {hqLevel}";
            timerText.text = upgradeRunning ? $"Upgrade: {remainingSeconds}s" : "Upgrade: Ready";
            statusText.text = !string.IsNullOrWhiteSpace(statusOverride)
                ? statusOverride
                : fallbackStatus;
            playHintText.text = $"Win reward: +{PlayerProgression.MinigameWinCoins} coins";

            // The collect button remains available in this prototype so the loop can be tested quickly.
            collectButton.interactable = true;

            // Prevent starting a second timer or spending coins that are not available.
            upgradeButton.interactable = !upgradeRunning && canAffordUpgrade;
            playButton.interactable = true;
        }
    }
}
