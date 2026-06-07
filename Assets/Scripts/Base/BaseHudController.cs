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
        private Text missionPanelTitleText;

        [SerializeField]
        private Text missionPanelText;

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
        private Button missionOneButton;

        [SerializeField]
        private Button missionTwoButton;

        [SerializeField]
        private Button missionThreeButton;

        [SerializeField]
        private Button missionFourButton;

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
            Text missionPanelTitle,
            Text missionPanel,
            Text heroPanelTitle,
            Text heroPanel,
            Text status,
            Text playHint,
            Button collect,
            Button upgrade,
            Button play,
            Button missionOne,
            Button missionTwo,
            Button missionThree,
            Button missionFour,
            Button reset,
            Button equipHero,
            Button heroes)
        {
            titleText = title;
            coinsText = coins;
            hqText = hq;
            timerText = timer;
            heroText = hero;
            missionPanelTitleText = missionPanelTitle;
            missionPanelText = missionPanel;
            heroPanelTitleText = heroPanelTitle;
            heroPanelText = heroPanel;
            statusText = status;
            playHintText = playHint;
            collectButton = collect;
            upgradeButton = upgrade;
            playButton = play;
            missionOneButton = missionOne;
            missionTwoButton = missionTwo;
            missionThreeButton = missionThree;
            missionFourButton = missionFour;
            resetButton = reset;
            equipHeroButton = equipHero;
            heroesButton = heroes;
        }

        public void Initialize(Action collectAction, Action upgradeAction, Action playAction, Action<int> selectMissionAction, Action resetAction, Action equipHeroAction, Action heroesAction)
        {
            // Replace listeners so scene rebuilds or test setup cannot accidentally duplicate clicks.
            collectButton.onClick.RemoveAllListeners();
            upgradeButton.onClick.RemoveAllListeners();
            playButton.onClick.RemoveAllListeners();
            missionOneButton.onClick.RemoveAllListeners();
            missionTwoButton.onClick.RemoveAllListeners();
            missionThreeButton.onClick.RemoveAllListeners();
            missionFourButton.onClick.RemoveAllListeners();
            resetButton.onClick.RemoveAllListeners();
            equipHeroButton.onClick.RemoveAllListeners();
            heroesButton.onClick.RemoveAllListeners();

            // Button listeners stay tiny and delegate all state changes to the bootstrap.
            collectButton.onClick.AddListener(() => collectAction?.Invoke());
            upgradeButton.onClick.AddListener(() => upgradeAction?.Invoke());
            playButton.onClick.AddListener(() => playAction?.Invoke());
            missionOneButton.onClick.AddListener(() => selectMissionAction?.Invoke(1));
            missionTwoButton.onClick.AddListener(() => selectMissionAction?.Invoke(2));
            missionThreeButton.onClick.AddListener(() => selectMissionAction?.Invoke(3));
            missionFourButton.onClick.AddListener(() => selectMissionAction?.Invoke(4));
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
            missionPanelTitleText.text = $"Missions {highestUnlockedMissionLevel}/{PlayerProgression.MaxMissionLevel}";
            missionPanelText.text = BuildMissionPanelText(saveData);
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
            ConfigureMissionButton(missionOneButton, saveData, 1, selectedMissionLevel);
            ConfigureMissionButton(missionTwoButton, saveData, 2, selectedMissionLevel);
            ConfigureMissionButton(missionThreeButton, saveData, 3, selectedMissionLevel);
            ConfigureMissionButton(missionFourButton, saveData, 4, selectedMissionLevel);
            equipHeroButton.interactable = HasUnequippedOwnedHero(ownedHeroes, equippedHero);
            heroesButton.interactable = true;
        }

        private static string BuildMissionPanelText(SaveGameData saveData)
        {
            // The panel always lists all authored local missions so locked goals are visible before they unlock.
            List<string> missionLines = new();
            int selectedMissionLevel = PlayerProgression.GetSelectedMissionLevel(saveData);
            for (int missionLevel = 1; missionLevel <= PlayerProgression.MaxMissionLevel; missionLevel += 1)
            {
                // A leading marker keeps selection visible even when a completed mission is selected for replay.
                string selectedMarker = missionLevel == selectedMissionLevel ? "> " : "  ";
                string missionName = PlayerProgression.GetMissionName(missionLevel);
                string statusLabel = PlayerProgression.GetMissionStatusLabel(saveData, missionLevel);
                string rewardHint = PlayerProgression.GetMissionRewardHint(saveData, missionLevel);
                missionLines.Add($"{selectedMarker}M{missionLevel} {missionName} [{statusLabel}] {rewardHint}");
            }

            return string.Join("\n", missionLines);
        }

        private static void ConfigureMissionButton(Button missionButton, SaveGameData saveData, int missionLevel, int selectedMissionLevel)
        {
            // Locked mission buttons are visible as goals but cannot be tapped in normal UI interaction.
            bool isMissionUnlocked = PlayerProgression.IsMissionUnlocked(saveData, missionLevel);
            missionButton.interactable = isMissionUnlocked;

            // Prefixing the selected button gives touch users a quick target check above the text panel.
            Text label = missionButton.GetComponentInChildren<Text>();
            if (label != null)
            {
                label.text = missionLevel == selectedMissionLevel ? $">M{missionLevel}" : $"M{missionLevel}";
            }
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
