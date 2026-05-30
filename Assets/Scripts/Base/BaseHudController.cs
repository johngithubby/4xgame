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
        private Button collectButton;

        [SerializeField]
        private Button upgradeButton;

        [SerializeField]
        private Button playButton;

        public void Configure(Text title, Text coins, Text hq, Text timer, Text status, Button collect, Button upgrade, Button play)
        {
            titleText = title;
            coinsText = coins;
            hqText = hq;
            timerText = timer;
            statusText = status;
            collectButton = collect;
            upgradeButton = upgrade;
            playButton = play;
        }

        public void Initialize(Action collectAction, Action upgradeAction, Action playAction)
        {
            // Replace listeners so scene rebuilds or test setup cannot accidentally duplicate clicks.
            collectButton.onClick.RemoveAllListeners();
            upgradeButton.onClick.RemoveAllListeners();
            playButton.onClick.RemoveAllListeners();

            // Button listeners stay tiny and delegate all state changes to the bootstrap.
            collectButton.onClick.AddListener(() => collectAction?.Invoke());
            upgradeButton.onClick.AddListener(() => upgradeAction?.Invoke());
            playButton.onClick.AddListener(() => playAction?.Invoke());
        }

        public void UpdateView(SaveGameData saveData, int remainingSeconds)
        {
            // Normalize the value read by UI so null data still shows a sensible first-launch state.
            int hqLevel = Mathf.Max(1, saveData?.hqLevel ?? 1);
            int coins = Mathf.Max(0, saveData?.coins ?? 0);
            int upgradeCost = PlayerProgression.GetHqUpgradeCost(hqLevel);
            bool upgradeRunning = saveData != null && saveData.hqUpgradeInProgress;
            bool canAffordUpgrade = coins >= upgradeCost;

            titleText.text = "Base";
            coinsText.text = $"Coins: {coins}";
            hqText.text = $"HQ Level: {hqLevel}";
            timerText.text = upgradeRunning ? $"Upgrade: {remainingSeconds}s" : "Upgrade: Ready";
            statusText.text = upgradeRunning
                ? "HQ upgrade in progress"
                : $"Next HQ upgrade: {upgradeCost} coins";

            // The collect button remains available in this prototype so the loop can be tested quickly.
            collectButton.interactable = true;

            // Prevent starting a second timer or spending coins that are not available.
            upgradeButton.interactable = !upgradeRunning && canAffordUpgrade;
            playButton.interactable = true;
        }
    }
}
