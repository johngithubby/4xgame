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
        private Text missionText;

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
        private Button previousMissionButton;

        [SerializeField]
        private Button nextMissionButton;

        [SerializeField]
        private Button resetButton;

        [SerializeField]
        private Button equipHeroButton;

        [SerializeField]
        private Button heroesButton;

        public void Configure(
            Text title,
            Text coins,
            Text hq,
            Text timer,
            Text hero,
            Text mission,
            Text heroPanelTitle,
            Text heroPanel,
            Text status,
            Text playHint,
            Button collect,
            Button upgrade,
            Button play,
            Button previousMission,
            Button nextMission,
            Button reset,
            Button equipHero,
            Button heroes)
        {
            titleText = title;
            coinsText = coins;
            hqText = hq;
            timerText = timer;
            heroText = hero;
            missionText = mission;
            heroPanelTitleText = heroPanelTitle;
            heroPanelText = heroPanel;
            statusText = status;
            playHintText = playHint;
            collectButton = collect;
            upgradeButton = upgrade;
            playButton = play;
            previousMissionButton = previousMission;
            nextMissionButton = nextMission;
            resetButton = reset;
            equipHeroButton = equipHero;
            heroesButton = heroes;
        }

        public void Initialize(Action collectAction, Action upgradeAction, Action playAction, Action previousMissionAction, Action nextMissionAction, Action resetAction, Action equipHeroAction, Action heroesAction)
        {
            // Replace listeners so scene rebuilds or test setup cannot accidentally duplicate clicks.
            collectButton.onClick.RemoveAllListeners();
            upgradeButton.onClick.RemoveAllListeners();
            playButton.onClick.RemoveAllListeners();
            previousMissionButton.onClick.RemoveAllListeners();
            nextMissionButton.onClick.RemoveAllListeners();
            resetButton.onClick.RemoveAllListeners();
            equipHeroButton.onClick.RemoveAllListeners();
            heroesButton.onClick.RemoveAllListeners();

            // Button listeners stay tiny and delegate all state changes to the bootstrap.
            collectButton.onClick.AddListener(() => collectAction?.Invoke());
            upgradeButton.onClick.AddListener(() => upgradeAction?.Invoke());
            playButton.onClick.AddListener(() => playAction?.Invoke());
            previousMissionButton.onClick.AddListener(() => previousMissionAction?.Invoke());
            nextMissionButton.onClick.AddListener(() => nextMissionAction?.Invoke());
            resetButton.onClick.AddListener(() => resetAction?.Invoke());
            equipHeroButton.onClick.AddListener(() => equipHeroAction?.Invoke());
            heroesButton.onClick.AddListener(() => heroesAction?.Invoke());

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
            int selectedMissionLevel = PlayerProgression.GetSelectedMissionLevel(saveData);
            int highestUnlockedMissionLevel = PlayerProgression.GetHighestUnlockedMissionLevel(saveData);
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
                ? $"Hero: {equippedHero.displayName} Lv {HeroInventory.GetHeroLevel(saveData, equippedHero.id)} (+{equippedHero.startingSquadBonus} squad)"
                : "Hero: None";
            missionText.text = $"Mission {selectedMissionLevel}: {PlayerProgression.GetMissionName(selectedMissionLevel)}\nUnlocked: {highestUnlockedMissionLevel}/{PlayerProgression.MaxMissionLevel}";
            heroPanelTitleText.text = "Owned Heroes";
            heroPanelText.text = BuildHeroPanelText(saveData, ownedHeroes, equippedHero);
            statusText.text = !string.IsNullOrWhiteSpace(statusOverride)
                ? statusOverride
                : fallbackStatus;

            // Before the first hero is owned, the play hint tells the player a hero can be earned.
            string missionUnlockHint = PlayerProgression.WouldUnlockNextMission(saveData) ? " + mission" : string.Empty;
            playHintText.text = firstHeroOwned
                ? $"Win reward: +{PlayerProgression.MinigameWinCoins} coins{missionUnlockHint}"
                : $"Win reward: +{PlayerProgression.MinigameWinCoins} coins + hero{missionUnlockHint}";

            // The collect button remains available in this prototype so the loop can be tested quickly.
            collectButton.interactable = true;

            // Prevent starting a second timer or spending coins that are not available.
            upgradeButton.interactable = !upgradeRunning && canAffordUpgrade;
            playButton.interactable = true;
            previousMissionButton.interactable = PlayerProgression.CanSelectPreviousMission(saveData);
            nextMissionButton.interactable = PlayerProgression.CanSelectNextMission(saveData);
            equipHeroButton.interactable = HasUnequippedOwnedHero(ownedHeroes, equippedHero);
            heroesButton.interactable = true;
        }

        private static string BuildHeroPanelText(SaveGameData saveData, IReadOnlyList<HeroDefinition> ownedHeroes, HeroDefinition equippedHero)
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
                // Each row shows the current local progression state for that owned hero.
                int heroLevel = HeroInventory.GetHeroLevel(saveData, hero.id);
                int heroXp = HeroInventory.GetHeroXp(saveData, hero.id);
                int xpToNextLevel = HeroProgression.GetXpRequiredForNextLevel(heroLevel);
                string equippedLabel = equippedHero != null && equippedHero.id == hero.id ? " EQ" : string.Empty;
                string xpLabel = xpToNextLevel > 0 ? $"{heroXp}/{xpToNextLevel}XP" : "MAX";
                heroLines.Add($"{hero.displayName} Lv{heroLevel} [{hero.rarity}]{equippedLabel} {xpLabel}");
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
