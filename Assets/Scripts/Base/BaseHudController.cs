using System;
using System.Collections.Generic;
using LaneSurvivor.Heroes;
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
        private Text heroText;

        [SerializeField]
        private Text heroPanelTitleText;

        [SerializeField]
        private Text heroPanelText;

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

        [SerializeField]
        private Button equipHeroButton;

        public void Configure(
            Text title,
            Text coins,
            Text hq,
            Text timer,
            Text hero,
            Text heroPanelTitle,
            Text heroPanel,
            Text status,
            Text playHint,
            Button collect,
            Button upgrade,
            Button play,
            Button reset,
            Button equipHero)
        {
            titleText = title;
            coinsText = coins;
            hqText = hq;
            timerText = timer;
            heroText = hero;
            heroPanelTitleText = heroPanelTitle;
            heroPanelText = heroPanel;
            statusText = status;
            playHintText = playHint;
            collectButton = collect;
            upgradeButton = upgrade;
            playButton = play;
            resetButton = reset;
            equipHeroButton = equipHero;
        }

        public void Initialize(Action collectAction, Action upgradeAction, Action playAction, Action resetAction, Action equipHeroAction)
        {
            // Replace listeners so scene rebuilds or test setup cannot accidentally duplicate clicks.
            collectButton.onClick.RemoveAllListeners();
            upgradeButton.onClick.RemoveAllListeners();
            playButton.onClick.RemoveAllListeners();
            resetButton.onClick.RemoveAllListeners();
            equipHeroButton.onClick.RemoveAllListeners();

            // Button listeners stay tiny and delegate all state changes to the bootstrap.
            collectButton.onClick.AddListener(() => collectAction?.Invoke());
            upgradeButton.onClick.AddListener(() => upgradeAction?.Invoke());
            playButton.onClick.AddListener(() => playAction?.Invoke());
            resetButton.onClick.AddListener(() => resetAction?.Invoke());
            equipHeroButton.onClick.AddListener(() => equipHeroAction?.Invoke());

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
            HeroDefinition equippedHero = HeroInventory.GetEquippedHero(saveData);
            IReadOnlyList<HeroDefinition> ownedHeroes = HeroInventory.GetOwnedHeroes(saveData);
            bool firstHeroOwned = HeroInventory.OwnsHero(saveData, HeroCatalog.FirstWinHeroId);

            // Build the default status separately so action feedback can override it cleanly.
            string fallbackStatus = upgradeRunning
                ? "HQ upgrade in progress"
                : $"Next HQ upgrade: {upgradeCost} coins";

            titleText.text = "Base";
            coinsText.text = $"Coins: {coins}";
            hqText.text = $"HQ Level: {hqLevel}";
            timerText.text = upgradeRunning ? $"Upgrade: {remainingSeconds}s" : "Upgrade: Ready";
            heroText.text = equippedHero != null
                ? $"Hero: {equippedHero.displayName} (+{equippedHero.startingSquadBonus} squad)"
                : "Hero: None";
            heroPanelTitleText.text = "Owned Heroes";
            heroPanelText.text = BuildHeroPanelText(ownedHeroes, equippedHero);
            statusText.text = !string.IsNullOrWhiteSpace(statusOverride)
                ? statusOverride
                : fallbackStatus;

            // Before the first hero is owned, the play hint tells the player a hero can be earned.
            playHintText.text = firstHeroOwned
                ? $"Win reward: +{PlayerProgression.MinigameWinCoins} coins"
                : $"Win reward: +{PlayerProgression.MinigameWinCoins} coins + hero";

            // The collect button remains available in this prototype so the loop can be tested quickly.
            collectButton.interactable = true;

            // Prevent starting a second timer or spending coins that are not available.
            upgradeButton.interactable = !upgradeRunning && canAffordUpgrade;
            playButton.interactable = true;
            equipHeroButton.interactable = HasUnequippedOwnedHero(ownedHeroes, equippedHero);
        }

        private static string BuildHeroPanelText(IReadOnlyList<HeroDefinition> ownedHeroes, HeroDefinition equippedHero)
        {
            // Empty ownership should be explicit because the panel exists before the first win reward.
            if (ownedHeroes == null || ownedHeroes.Count == 0)
            {
                return "None earned yet";
            }

            // Keep the panel compact enough for the placeholder mobile HUD.
            List<string> heroLines = new();
            foreach (HeroDefinition hero in ownedHeroes)
            {
                string equippedLabel = equippedHero != null && equippedHero.id == hero.id ? " EQ" : string.Empty;
                heroLines.Add($"{hero.displayName} [{hero.rarity}]{equippedLabel} +{hero.startingSquadBonus}/+{hero.damageBonus:0.##}");
            }

            return string.Join("\n", heroLines);
        }

        private static bool HasUnequippedOwnedHero(IReadOnlyList<HeroDefinition> ownedHeroes, HeroDefinition equippedHero)
        {
            // Without owned heroes there is nothing for the Base panel button to equip.
            if (ownedHeroes == null || ownedHeroes.Count == 0)
            {
                return false;
            }

            // One owned but unequipped hero can be equipped; multiple heroes can cycle selection.
            return equippedHero == null || ownedHeroes.Count > 1;
        }
    }
}
